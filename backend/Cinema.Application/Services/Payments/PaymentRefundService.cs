using Cinema.Application.Events;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.CounterShifts;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Cinema.Application.Services.Payments;

public class PaymentRefundService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IEnumerable<IPaymentGateway> paymentGateways,
    IBusinessEventDispatcher eventDispatcher,
    IAuditLogger auditLogger,
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

    public Task<PaymentTransaction> ReserveAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        int? performedByUserId,
        CancellationToken cancellationToken
    ) => ReserveCoreAsync(charge, amount, note, performedByUserId, false, false, cancellationToken);

    public Task<PaymentTransaction> RecordManualAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        int performedByUserId,
        CancellationToken cancellationToken
    ) => ReserveCoreAsync(charge, amount, note, performedByUserId, false, true, cancellationToken);

    public Task<PaymentTransaction> ReserveManualCashAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        CancellationToken cancellationToken
    )
    {
        if (charge.Method != PaymentMethod.Cash)
            throw new BusinessRuleException(Messages.Refund_CashOnly);

        return ReserveCoreAsync(charge, amount, note, null, true, false, cancellationToken);
    }

    private async Task<PaymentTransaction> ReserveCoreAsync(
        PaymentTransaction charge,
        decimal amount,
        string? note,
        int? performedByUserId,
        bool manualCash,
        bool completedManually,
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
        var isPending = !completedManually && (manualCash || charge.Method.IsGatewayMethod());
        int? counterShiftId =
            !isPending && charge.Method == PaymentMethod.Cash && performedByUserId is { } performer
                ? await db.FindOpenCounterShiftIdByUserAsync(performer, cancellationToken)
                : null;
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
            NeedsManualRefund = manualCash,
            CounterShiftId = counterShiftId,
            Status = isPending ? PaymentStatus.Pending : PaymentStatus.Success,
            CreatedAt = now,
            CompletedAt = isPending ? null : now,
        };
        await db.InTransactionAsync(
            async () =>
            {
                db.PaymentTransactions.Add(refund);
                await db.SaveChangesAsync(cancellationToken);
                if (!isPending)
                    await PublishRefundedAsync(refund, cancellationToken);
            },
            cancellationToken
        );
        return refund;
    }

    public Task<PaymentRefundResult> ExecuteAsync(
        int refundTransactionId,
        CancellationToken cancellationToken
    ) => ExecuteAsync(refundTransactionId, true, cancellationToken);

    public async Task<PaymentRefundResult> ExecuteAsync(
        int refundTransactionId,
        bool failOnError,
        CancellationToken cancellationToken
    )
    {
        PaymentRefundResult? result = null;
        await db.InTransactionAsync(
            async () =>
            {
                result = await ExecuteLockedAsync(
                    refundTransactionId,
                    failOnError,
                    null,
                    cancellationToken
                );
            },
            cancellationToken
        );
        return result!;
    }

    public async Task<PaymentRefundResult> RetryPendingAsync(
        int refundTransactionId,
        DateTime notAttemptedSinceUtc,
        CancellationToken cancellationToken
    )
    {
        PaymentRefundResult? result = null;
        await db.InTransactionAsync(
            async () =>
            {
                result = await ExecuteLockedAsync(
                    refundTransactionId,
                    false,
                    notAttemptedSinceUtc,
                    cancellationToken
                );
            },
            cancellationToken
        );
        return result!;
    }

    public async Task<PaymentRefundResult> ConfirmCashRefundAsync(
        int refundTransactionId,
        int confirmedByUserId,
        CancellationToken cancellationToken
    )
    {
        PaymentRefundResult? result = null;
        await db.InTransactionAsync(
            async () =>
            {
                await db.LockTransactionAsync(refundTransactionId, cancellationToken);
                var refund =
                    await db
                        .PaymentTransactions.AsNoTracking()
                        .FirstOrDefaultAsync(
                            t =>
                                t.PaymentTransactionId == refundTransactionId
                                && t.Type == PaymentTransactionType.Refund,
                            cancellationToken
                        ) ?? throw new NotFoundException(Messages.NotFound_Payment);

                if (refund.Method != PaymentMethod.Cash)
                    throw new BusinessRuleException(Messages.Refund_CashOnly);
                if (refund.Status == PaymentStatus.Success)
                {
                    result = ToResult(refund, refund.Note);
                    return;
                }

                if (refund.Status != PaymentStatus.Pending)
                    throw new BusinessRuleException(Messages.Refund_CashNotConfirmable);

                var counterShiftId = await db.FindOpenCounterShiftIdByUserAsync(
                    confirmedByUserId,
                    cancellationToken
                );
                if (
                    counterShiftId is null
                    && await db.Employees.AnyAsync(
                        e => e.UserId == confirmedByUserId,
                        cancellationToken
                    )
                )
                    throw new BusinessRuleException(Messages.Rule_CounterShiftRequired);

                var completedAt = DateTime.UtcNow;
                var updated = await db
                    .PaymentTransactions.Where(t =>
                        t.PaymentTransactionId == refundTransactionId
                        && t.Status == PaymentStatus.Pending
                    )
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(t => t.Status, PaymentStatus.Success)
                                .SetProperty(t => t.CompletedAt, (DateTime?)completedAt)
                                .SetProperty(t => t.PerformedByUserId, (int?)confirmedByUserId)
                                .SetProperty(t => t.CounterShiftId, counterShiftId)
                                .SetProperty(t => t.NeedsManualRefund, false),
                        cancellationToken
                    );
                if (updated == 0)
                {
                    result = ToResult(refund, refund.Note);
                    return;
                }

                refund.Status = PaymentStatus.Success;
                refund.CompletedAt = completedAt;
                refund.PerformedByUserId = confirmedByUserId;
                refund.CounterShiftId = counterShiftId;
                refund.NeedsManualRefund = false;

                auditLogger.Record(
                    AuditActions.ManualRefund,
                    AuditEntityTypes.PaymentTransaction,
                    refundTransactionId.ToString(),
                    AuditSnapshots.CashRefundBeforeConfirm(),
                    AuditSnapshots.CashRefundAfterConfirm(refund, confirmedByUserId)
                );
                await db.SaveChangesAsync(cancellationToken);
                await PublishRefundedAsync(refund, cancellationToken);
                result = ToResult(refund, refund.Note);
            },
            cancellationToken
        );
        return result!;
    }

    private async Task<PaymentRefundResult> ExecuteLockedAsync(
        int refundTransactionId,
        bool failOnError,
        DateTime? notAttemptedSinceUtc,
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

        if (refund.Status != PaymentStatus.Pending || refund.Method == PaymentMethod.Cash)
            return ToResult(refund, refund.Note);

        if (
            notAttemptedSinceUtc is { } attemptedBefore
            && refund.LastRefundAttemptAt is { } lastAttempt
            && lastAttempt >= attemptedBefore
        )
            return ToResult(refund, refund.Note);

        var charge = refund.OriginalTransaction!;
        var baseNote = BaseNote(refund.Note);
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
                        string.IsNullOrWhiteSpace(baseNote)
                            ? string.Format(
                                Messages.Refund_TransactionDescription,
                                charge.TransactionCode
                            )
                            : baseNote,
                        charge.CreatedAt,
                        GatewayTransactionId: charge.GatewayTransactionId,
                        OriginalAmount: charge.Amount,
                        IdempotencyKey: IdempotencyKeyFor(refund)
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
        var definitiveFailure = gateway is null || gatewayResult is { Success: false, Rejected: true };
        var status = success
            ? PaymentStatus.Success
            : failOnError && definitiveFailure
                ? PaymentStatus.Failed
                : PaymentStatus.Pending;
        DateTime? completedValue = status == PaymentStatus.Pending ? null : completedAt;
        var gatewayTransactionId = success
            ? gatewayResult!.RefundTransactionId
            : refund.GatewayTransactionId;
        var note = success
            ? baseNote
            : Truncate(
                string.Join(
                    NoteSeparator,
                    new[]
                    {
                        baseNote,
                        definitiveFailure ? null : Messages.Refund_OutcomeUnknown,
                        gatewayResult?.Message,
                        message,
                    }.Where(s =>
                        !string.IsNullOrWhiteSpace(s)
                    )
                )
            );

        if (!success)
            logger.LogWarning(
                "RefundNotCompleted RefundTransactionId {RefundId} ChargeTransactionId {ChargeId} Amount {Amount} Status {Status} Definitive {Definitive}",
                refund.PaymentTransactionId,
                charge.PaymentTransactionId,
                refund.Amount,
                status,
                definitiveFailure
            );

        var attemptCount = refund.RefundAttemptCount + 1;
        DateTime? lastAttemptAt = completedAt;

        var updated = await db
            .PaymentTransactions.Where(t =>
                t.PaymentTransactionId == refundTransactionId && t.Status == PaymentStatus.Pending
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(t => t.Status, status)
                        .SetProperty(t => t.CompletedAt, completedValue)
                        .SetProperty(t => t.GatewayTransactionId, gatewayTransactionId)
                        .SetProperty(t => t.Note, note)
                        .SetProperty(t => t.RefundAttemptCount, attemptCount)
                        .SetProperty(t => t.LastRefundAttemptAt, lastAttemptAt),
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

    private static PaymentRefundResult ToResult(PaymentTransaction refund, string? message) =>
        new(refund.PaymentTransactionId, refund.Status, refund.Amount, message);

    private const string NoteSeparator = " | ";

    private static string? BaseNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return note;
        var index = note.IndexOf(NoteSeparator, StringComparison.Ordinal);
        return index < 0 ? note : note[..index];
    }

    private static string IdempotencyKeyFor(PaymentTransaction refund) =>
        Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refund.TransactionCode)))[..32]
            .ToLowerInvariant();

    private static string? Truncate(string? value) =>
        value is { Length: > FieldLengths.ErrorMessage } ? value[..FieldLengths.ErrorMessage] : value;
}
