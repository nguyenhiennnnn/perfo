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
        db.PaymentTransactions.Add(refund);
        await db.SaveChangesAsync(cancellationToken);
        return refund;
    }

    public async Task<PaymentRefundResult> ExecuteAsync(
        int refundTransactionId,
        CancellationToken cancellationToken
    )
    {
        var refund =
            await db
                .PaymentTransactions.Include(t => t.OriginalTransaction)
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

        refund.CompletedAt = DateTime.UtcNow;
        if (gatewayResult is { Success: true })
        {
            refund.Status = PaymentStatus.Success;
            refund.GatewayTransactionId = gatewayResult.RefundTransactionId;
        }
        else
        {
            refund.Status = PaymentStatus.Failed;
            refund.Note = Truncate(
                string.Join(
                    " | ",
                    new[] { refund.Note, gatewayResult?.Message, message }.Where(s =>
                        !string.IsNullOrWhiteSpace(s)
                    )
                )
            );
            logger.LogWarning(
                "RefundFailed RefundTransactionId {RefundId} ChargeTransactionId {ChargeId} Amount {Amount}",
                refund.PaymentTransactionId,
                charge.PaymentTransactionId,
                refund.Amount
            );
        }

        await db.InTransactionAsync(
            async () =>
            {
                await db.SaveChangesAsync(cancellationToken);
                if (refund.Status == PaymentStatus.Success)
                    await eventDispatcher.PublishAsync(
                        new OrderRefunded(
                            refund.OrderId,
                            refund.PaymentTransactionId,
                            refund.Amount,
                            refund.CompletedAt ?? DateTime.UtcNow
                        ),
                        cancellationToken
                    );
            },
            cancellationToken
        );
        return ToResult(refund, gatewayResult?.Message ?? message);
    }

    private IPaymentGateway? FindGateway(PaymentMethod method) =>
        paymentGateways.FindByMethod(method.ToString());

    private static bool IsGatewayMethod(PaymentMethod method) =>
        method is PaymentMethod.VNPay or PaymentMethod.Momo or PaymentMethod.Stripe;

    private static PaymentRefundResult ToResult(PaymentTransaction refund, string? message) =>
        new(refund.PaymentTransactionId, refund.Status, refund.Amount, message);

    private static string? Truncate(string? value) =>
        value is { Length: > FieldLengths.ErrorMessage } ? value[..FieldLengths.ErrorMessage] : value;
}
