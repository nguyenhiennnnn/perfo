using Cinema.Application.Caching;
using Cinema.Application.DTOs.Memberships;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Memberships;

public class MembershipRefundService(
    IAppDbContext db,
    IAppCache cache,
    IMembershipRevenueService revenue,
    IOrderRefundService orderRefunds,
    IOrderCancellationFinalizer finalizer,
    IAuditLogger auditLogger,
    ILogger<MembershipRefundService> logger
) : IMembershipRefundService
{
    public async Task<MembershipRefundResultDto> RefundAsync(
        int subscriptionId,
        CancelActor actor,
        MembershipRefundReason reason,
        string? note,
        CancellationToken cancellationToken
    )
    {
        if (reason != MembershipRefundReason.CustomerRequest && actor.Kind != CancelActorKind.Admin)
            throw new ForbiddenAppException(Messages.Forbidden_MembershipRefundAdminOnly);

        var owner =
            await db
                .MembershipSubscriptions.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => s.MembershipSubscriptionId == subscriptionId)
                .Select(s => new
                {
                    s.UserId,
                    s.OrderId,
                    s.Order.OrderCode,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_MembershipSubscription);
        if (actor.IsCustomer && owner.UserId != actor.UserId)
            throw new ForbiddenAppException(Messages.Forbidden_AccessDenied);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        IReadOnlyList<OrderRefundReservation> reservations;
        IReadOnlyList<PaymentRefundResult> results;
        MembershipRevenueSettlement settlement;
        var wholeOrder = false;
        var revoked = 0;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockOrderAsync(owner.OrderId, cancellationToken);

        var current =
            await db
                .MembershipSubscriptions.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s => s.MembershipSubscriptionId == subscriptionId)
                .Select(s => new
                {
                    s.Status,
                    s.ValidFrom,
                    s.ValidTo,
                })
                .FirstAsync(cancellationToken);
        if (current.Status != MembershipSubscriptionStatus.Active)
            throw new BusinessRuleException(Messages.Rule_MembershipSubscriptionNotRefundable);

        var now = DateTime.UtcNow;
        var started = current.ValidFrom <= now;
        if (
            started
            && reason == MembershipRefundReason.CustomerRequest
            && !settings.MembershipRefundStartedEnabled
        )
            throw new BusinessRuleException(Messages.Rule_MembershipStartedNotRefundable);

        if (
            await db
                .MembershipFreeTickets.IgnoreQueryFilters()
                .AnyAsync(
                    t =>
                        t.MembershipSubscriptionId == subscriptionId
                        && t.OrderId != null
                        && t.UsedAt == null
                        && t.RevokedAt == null,
                    cancellationToken
                )
        )
            throw new BusinessRuleException(Messages.Rule_MembershipRefundFreeTicketHeld);

        settlement = await revenue.SettleForRefundAsync(subscriptionId, cancellationToken);
        if (settlement.UnrecognizedAmount <= 0m)
            throw new BusinessRuleException(Messages.Rule_MembershipRefundNothingLeft);

        var fullPlan = await orderRefunds.BuildPlanAsync(
            owner.OrderId,
            RefundScope.Full(false),
            cancellationToken
        );
        if (fullPlan.TotalAmount <= 0m)
            throw new BusinessRuleException(Messages.Rule_MembershipRefundNothingLeft);

        var amount = Math.Min(settlement.UnrecognizedAmount, fullPlan.TotalAmount);
        wholeOrder =
            settlement.UnrecognizedMonths == settlement.TotalMonths
            && amount == fullPlan.TotalAmount;
        var plan = wholeOrder
            ? fullPlan
            : await orderRefunds.BuildPlanForAmountAsync(owner.OrderId, amount, cancellationToken);

        var claimed = await db
            .MembershipSubscriptions.Where(s =>
                s.MembershipSubscriptionId == subscriptionId
                && s.Status == MembershipSubscriptionStatus.Active
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(s => s.Status, MembershipSubscriptionStatus.Refunded)
                        .SetProperty(s => s.RefundedAt, (DateTime?)now)
                        .SetProperty(s => s.RefundReason, (MembershipRefundReason?)reason)
                        .SetProperty(s => s.StoppedRevenueAmount, settlement.UnrecognizedAmount),
                cancellationToken
            );
        if (claimed == 0)
            throw new ConflictAppException(Messages.Db_ConcurrencyConflict);

        if (!wholeOrder)
        {
            var updated = await db
                .OrderLines.Where(l =>
                    l.OrderId == owner.OrderId
                    && l.LineType == OrderLineType.Fee
                    && l.MembershipPlanId != null
                    && l.RefundedAmount + amount <= l.PaidAmount
                )
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(l => l.RefundedAmount, l => l.RefundedAmount + amount),
                    cancellationToken
                );
            if (updated == 0)
                throw new BusinessRuleException(Messages.Rule_MembershipRefundNothingLeft);
        }

        reservations = await orderRefunds.ReserveAsync(
            plan,
            actor.UserId,
            string.Format(Messages.Refund_MembershipDescription, owner.OrderCode),
            cancellationToken
        );
        results = await orderRefunds.ExecuteAsync(reservations, cancellationToken);

        var gatewayIds = reservations
            .Where(r => r.Method.IsGatewayMethod())
            .Select(r => r.RefundTransactionId)
            .ToHashSet();
        var gatewayResults = results.Where(r => gatewayIds.Contains(r.RefundTransactionId)).ToList();
        if (gatewayResults.Count > 0 && gatewayResults.All(r => r.IsFailed))
        {
            var message =
                gatewayResults.Select(r => r.Message).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? Messages.Refund_ExecutionError;
            logger.LogWarning(
                "MembershipRefundFailed MembershipSubscriptionId {MembershipSubscriptionId} Message {Message}",
                subscriptionId,
                message
            );
            throw new BusinessRuleException(string.Format(Messages.Rule_RefundFailed, message));
        }

        var gatewayRefunded = gatewayResults.Where(r => r.Success).ToList();
        try
        {
            var failedIds = results.Where(r => r.IsFailed).Select(r => r.RefundTransactionId).ToList();
            await MarkManualRefundsAsync(owner.OrderId, reservations, failedIds, cancellationToken);

            revoked = await db
                .MembershipFreeTickets.IgnoreQueryFilters()
                .Where(t =>
                    t.MembershipSubscriptionId == subscriptionId
                    && t.OrderId == null
                    && t.UsedAt == null
                    && t.RevokedAt == null
                )
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(t => t.RevokedAt, (DateTime?)now),
                    cancellationToken
                );

            if (wholeOrder)
                await finalizer.FinalizeAsync(
                    owner.OrderId,
                    CancelReasonFor(reason),
                    actor.CancelledBy,
                    plan,
                    cancellationToken
                );

            var refunded = results.Where(r => r.Success).Sum(r => r.Amount);
            var manual = results.Where(r => !r.Success).Sum(r => r.Amount);
            auditLogger.Record(
                AuditActions.RefundMembershipSubscription,
                AuditEntityTypes.MembershipSubscription,
                subscriptionId.ToString(),
                new { Status = MembershipSubscriptionStatus.Active },
                new
                {
                    Status = MembershipSubscriptionStatus.Refunded,
                    owner.OrderId,
                    Reason = reason,
                    Note = trimmedNote,
                    Started = started,
                    StoppedMonths = settlement.UnrecognizedMonths,
                    StoppedRevenueAmount = settlement.UnrecognizedAmount,
                    Refunded = refunded,
                    ManualRefund = manual,
                    RevokedFreeTickets = revoked,
                    OrderCancelled = wholeOrder,
                    ActorUserId = actor.UserId,
                }
            );

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (gatewayRefunded.Count > 0)
        {
            logger.LogCritical(
                ex,
                "MembershipRefundCommitFailed MembershipSubscriptionId {MembershipSubscriptionId} RefundTransactionIds {RefundIds} — đã hoàn tiền ở cổng nhưng DB chưa ghi nhận.",
                subscriptionId,
                string.Join(",", gatewayRefunded.Select(r => r.RefundTransactionId))
            );
            throw;
        }

        var refunds = reservations
            .Select(r =>
            {
                var result = results.First(x => x.RefundTransactionId == r.RefundTransactionId);
                return new OrderCancellationRefund(
                    r.RefundTransactionId,
                    r.Method,
                    result.Amount,
                    result.Status
                );
            })
            .ToList();
        var totalRefunded = refunds.Where(r => r.Status == PaymentStatus.Success).Sum(r => r.Amount);

        logger.LogInformation(
            "MembershipSubscriptionRefunded MembershipSubscriptionId {MembershipSubscriptionId} ActorUserId {UserId} Reason {Reason} StoppedMonths {StoppedMonths} Refunded {Refunded} RevokedFreeTickets {Revoked}",
            subscriptionId,
            actor.UserId,
            reason,
            settlement.UnrecognizedMonths,
            totalRefunded,
            revoked
        );

        var dtoRefunds = refunds
            .GroupBy(r => new { r.Method, r.Status })
            .Select(g => new CancelOrderRefundDto(
                g.Key.Method.ToDtoMethod(),
                g.Sum(r => r.Amount),
                g.Key.Status.ToDtoStatus()
            ))
            .ToList();
        var refundStatus =
            refunds.Count == 0 ? CancelOrderResponse.RefundStatusNone
            : refunds.All(r => r.Status == PaymentStatus.Success) ? CancelOrderResponse.RefundStatusRefunded
            : CancelOrderResponse.RefundStatusPending;
        return new MembershipRefundResultDto(
            subscriptionId,
            owner.OrderId,
            owner.OrderCode,
            wholeOrder,
            settlement.UnrecognizedAmount,
            settlement.UnrecognizedMonths,
            revoked,
            totalRefunded,
            refundStatus,
            dtoRefunds
        );
    }

    private static CancelReason CancelReasonFor(MembershipRefundReason reason) =>
        reason switch
        {
            MembershipRefundReason.CustomerRequest => CancelReason.CustomerChangedMind,
            MembershipRefundReason.SystemError => CancelReason.PaymentError,
            _ => CancelReason.Other,
        };

    private async Task MarkManualRefundsAsync(
        int orderId,
        IReadOnlyList<OrderRefundReservation> reservations,
        IReadOnlyList<int> failedRefundIds,
        CancellationToken cancellationToken
    )
    {
        var chargeIds = reservations
            .Where(r => failedRefundIds.Contains(r.RefundTransactionId))
            .Select(r => r.ChargeTransactionId)
            .Distinct()
            .ToList();
        if (chargeIds.Count == 0)
            return;

        await db
            .PaymentTransactions.Where(t => chargeIds.Contains(t.PaymentTransactionId))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.NeedsManualRefund, true),
                cancellationToken
            );
        logger.LogCritical(
            "MembershipRefundNeedsManual OrderId {OrderId} ChargeTransactionIds {ChargeIds} — cần hoàn tiền thủ công.",
            orderId,
            string.Join(",", chargeIds)
        );
    }
}
