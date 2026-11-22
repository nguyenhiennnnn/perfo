using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Payments;

public class PaymentRefundService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IEnumerable<IPaymentGateway> paymentGateways,
    IBusinessEventDispatcher eventDispatcher,
    ILogger<PaymentRefundService> logger
) : IPaymentRefundService
{
    public async Task<PaymentRefundResult> RefundAsync(
        int chargeTransactionId,
        decimal amount,
        string? note,
        int? performedByUserId,
        CancellationToken cancellationToken
    )
    {
        PaymentTransaction refund;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.LockTransactionAsync(chargeTransactionId, cancellationToken);
            var charge =
                await db
                    .PaymentTransactions.AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.PaymentTransactionId == chargeTransactionId,
                        cancellationToken
                    ) ?? throw new NotFoundException(Messages.NotFound_Payment);

            refund = await ReserveAsync(charge, amount, note, performedByUserId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return await ExecuteAsync(refund.PaymentTransactionId, cancellationToken);
    }

    public async Task<PaymentTransaction> ReserveAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        int? performedByUserId,
        CancellationToken cancellationToken
    )
    {
        amount = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (amount <= 0)
            throw new BusinessRuleException(Messages.Refund_AmountInvalid);

        if (charge.Type != PaymentTransactionType.Charge || charge.Status != PaymentStatus.Success)
            throw new BusinessRuleException(Messages.Refund_OriginalNotRefundable);

        if (charge.Method == PaymentMethod.GiftCard)
            throw new BusinessRuleException(Messages.Refund_MethodUnsupported);

        var refundable = await ledger.GetRefundableAmountAsync(
            charge.PaymentTransactionId,
            cancellationToken
        );
        if (amount > refundable)
            throw new BusinessRuleException(Messages.Refund_ExceedsCollected);

        var now = DateTime.UtcNow;
        var isGateway = IsGatewayMethod(charge.Method);
        var refund = new PaymentTransaction
        {
            OrderId = charge.OrderId,
            Type = PaymentTransactionType.Refund,
            Method = charge.Method,
            Amount = amount,
            TransactionCode = CodeGenerator.GenerateRefundTransactionCode(charge.TransactionCode),
            OriginalTransactionId = charge.PaymentTransactionId,
            PerformedByUserId = performedByUserId,
            Note = Truncate(note),
            Status = isGateway ? PaymentStatus.Pending : PaymentStatus.Success,
            CreatedAt = now,
            CompletedAt = isGateway ? null : now,
        };
        await db.InTransactionAsync(
            async () =>
            {
                db.PaymentTransactions.Add(refund);
                await db.SaveChangesAsync(cancellationToken);
                if (!isGateway)
                    await PublishRefundedAsync(refund, cancellationToken);
            },
            cancellationToken
        );
        return refund;
    }

    public async Task<PaymentRefundResult> ExecuteAsync(
        int refundTransactionId,
        CancellationToken cancellationToken
    )
    {
        PaymentRefundResult? result = null;
        await db.InTransactionAsync(
            async () =>
            {
                result = await ExecuteLockedAsync(refundTransactionId, cancellationToken);
            },
            cancellationToken
        );
        return result!;
    }

    private async Task<PaymentRefundResult> ExecuteLockedAsync(
        int refundTransactionId,
        CancellationToken cancellationToken
    )
    {
        await db.LockTransactionAsync(refundTransactionId, cancellationToken);
        var refund =
            await db
                .PaymentTransactions.AsNoTracking()
                .Include(t => t.OriginalTransaction)
                .FirstOrDefaultAsync(
                    t =>
                        t.PaymentTransactionId == refundTransactionId
                        && t.Type == PaymentTransactionType.Refund,
                    cancellationToken
                ) ?? throw new NotFoundException(Messages.NotFound_Payment);

        if (refund.Status != PaymentStatus.Pending)
            return ToResult(refund, refund.Note);

        var charge = refund.OriginalTransaction!;
        var gateway = FindGateway(refund.Method);
        RefundResult? gatewayResult = null;
        string? message = null;

        if (gateway is null)
        {
            message = string.Format(Messages.Refund_GatewayUnavailable, refund.Method);
        }
        else
        {
            try
            {
                gatewayResult = await gateway.RefundAsync(
                    new RefundRequest(
                        charge.TransactionCode,
                        refund.Amount,
                        string.IsNullOrWhiteSpace(refund.Note)
                            ? string.Format(
                                Messages.Refund_TransactionDescription,
                                charge.TransactionCode
                            )
                            : refund.Note,
                        charge.CreatedAt,
                        GatewayTransactionId: charge.GatewayTransactionId,
                        OriginalAmount: charge.Amount
                    ),
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "RefundGatewayError RefundTransactionId {RefundId} ChargeTransactionId {ChargeId}",
                    refund.PaymentTransactionId,
                    charge.PaymentTransactionId
                );
                message = ex.Message;
            }
        }

        var completedAt = DateTime.UtcNow;
        var success = gatewayResult is { Success: true };
        var status = success ? PaymentStatus.Success : PaymentStatus.Failed;
        var gatewayTransactionId = success
            ? gatewayResult!.RefundTransactionId
            : refund.GatewayTransactionId;
        var note = success
            ? refund.Note
            : Truncate(
                string.Join(
                    " | ",
                    new[] { refund.Note, gatewayResult?.Message, message }.Where(s =>
                        !string.IsNullOrWhiteSpace(s)
                    )
                )
            );

        if (!success)
            logger.LogWarning(
                "RefundFailed RefundTransactionId {RefundId} ChargeTransactionId {ChargeId} Amount {Amount}",
                refund.PaymentTransactionId,
                charge.PaymentTransactionId,
                refund.Amount
            );

        var updated = await db
            .PaymentTransactions.Where(t =>
                t.PaymentTransactionId == refundTransactionId && t.Status == PaymentStatus.Pending
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(t => t.Status, status)
                        .SetProperty(t => t.CompletedAt, completedAt)
                        .SetProperty(t => t.GatewayTransactionId, gatewayTransactionId)
                        .SetProperty(t => t.Note, note),
                cancellationToken
            );

        if (updated > 0 && success)
        {
            refund.CompletedAt = completedAt;
            await PublishRefundedAsync(refund, cancellationToken);
        }

        refund.Status = status;
        return ToResult(refund, gatewayResult?.Message ?? message);
    }

    private Task PublishRefundedAsync(
        PaymentTransaction refund,
        CancellationToken cancellationToken
    ) =>
        eventDispatcher.PublishAsync(
            new OrderRefunded(
                refund.OrderId,
                refund.PaymentTransactionId,
                refund.Amount,
                refund.CompletedAt ?? DateTime.UtcNow
            ),
            cancellationToken
        );

    private IPaymentGateway? FindGateway(PaymentMethod method) =>
        paymentGateways.FindByMethod(method.ToString());

    private static bool IsGatewayMethod(PaymentMethod method) =>
        method is PaymentMethod.VNPay or PaymentMethod.Momo or PaymentMethod.Stripe;

    private static PaymentRefundResult ToResult(PaymentTransaction refund, string? message) =>
        new(refund.PaymentTransactionId, refund.Status, refund.Amount, message);

    private static string? Truncate(string? value) =>
        value is { Length: > FieldLengths.ErrorMessage } ? value[..FieldLengths.ErrorMessage] : value;
}
