using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderCancellationService(
    IAppDbContext db,
    IAppCache cache,
    IOrderReleaseService releaseService,
    IOrderRefundService orderRefunds,
    IOrderCancellationFinalizer finalizer,
    IRealtimeNotifier realtimeNotifier,
    IAuditLogger auditLogger,
    ILogger<OrderCancellationService> logger
) : IOrderCancellationService
{
    public async Task<OrderCancellationResult> CancelAsync(
        int orderId,
        CancelActor actor,
        CancelReason reason,
        CancellationToken cancellationToken
    )
    {
        OrderCancellationRules.EnsureReasonAllowed(actor.Kind, reason);

        var order =
            await db
                .Orders.AsNoTracking()
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderCode,
                    o.UserId,
                    o.Status,
                    o.Type,
                })
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        if (actor.IsCustomer && order.UserId != actor.UserId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

        if (order.Type == OrderType.Membership && order.Status == OrderStatus.Confirmed)
            throw new BusinessRuleException(Messages.Rule_MembershipOrderCancelViaSubscription);

        return order.Status switch
        {
            OrderStatus.Pending => await CancelPendingAsync(
                order.OrderId,
                order.OrderCode,
                actor,
                reason,
                cancellationToken
            ),
            OrderStatus.Confirmed => await CancelConfirmedAsync(
                order.OrderId,
                order.OrderCode,
                actor,
                reason,
                cancellationToken
            ),
            _ => throw new BusinessRuleException(Messages.Rule_BookingNotCancellable),
        };
    }

    private async Task<OrderCancellationResult> CancelPendingAsync(
        int orderId,
        string orderCode,
        CancelActor actor,
        CancelReason reason,
        CancellationToken cancellationToken
    )
    {
        if (reason == CancelReason.RoomClosed)
            await EnsureRoomChangeCancellableAsync(orderId, cancellationToken);

        var cancelled = await releaseService.CancelPendingAsync(
            orderId,
            reason,
            actor.CancelledBy,
            cancellationToken
        );
        if (!cancelled)
            throw new ConflictAppException(Messages.Conflict_BookingBeingProcessedOrCancelled);

        if (ShouldAudit(actor, reason))
        {
            RecordAudit(actor, orderId, OrderStatus.Pending, reason, 0m, 0m);
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "OrderCancelled(Pending) OrderId {OrderId} ActorKind {Kind} ActorUserId {UserId}",
            orderId,
            actor.Kind,
            actor.UserId
        );
        return new OrderCancellationResult(orderId, orderCode, OrderStatus.Cancelled, 0m, []);
    }

    private async Task<OrderCancellationResult> CancelConfirmedAsync(
        int orderId,
        string orderCode,
        CancelActor actor,
        CancelReason reason,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);

        IReadOnlyList<OrderRefundReservation> reservations = [];
        IReadOnlyList<ReleasedSeat> releasedSeats = [];

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.LockOrderAsync(orderId, cancellationToken);

            var status = await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => o.Status)
                .FirstAsync(cancellationToken);
            if (status != OrderStatus.Confirmed)
                throw new ConflictAppException(Messages.Conflict_BookingBeingProcessedOrCancelled);

            await EnsureCancellableAsync(orderId, actor, reason, settings, cancellationToken);

            var isRoomChangeCancel = reason == CancelReason.RoomClosed;
            var plan = await orderRefunds.BuildPlanAsync(
                orderId,
                isRoomChangeCancel
                    ? RefundScope.Full(true)
                    : RefundScope.ByPercent(settings.CancelRefundPercent),
                cancellationToken
            );
            reservations = await orderRefunds.ReserveAsync(
                plan,
                actor.UserId,
                string.Format(
                    isRoomChangeCancel
                        ? Messages.Refund_RoomChangeCancelDescription
                        : Messages.Refund_CancelOrderDescription,
                    orderCode
                ),
                cancellationToken
            );

            var voidedDues = ShouldAudit(actor, reason)
                ? await db
                    .RoomChangeRefundDues.AsNoTracking()
                    .Where(d =>
                        d.OrderId == orderId && d.Status == RoomChangeRefundStatus.Pending
                    )
                    .Select(d => d.AmountDue)
                    .ToListAsync(cancellationToken)
                : new List<decimal>();

            releasedSeats = await finalizer.FinalizeAsync(
                orderId,
                reason,
                actor.CancelledBy,
                plan,
                cancellationToken
            );

            if (ShouldAudit(actor, reason))
                RecordAudit(
                    actor,
                    orderId,
                    OrderStatus.Confirmed,
                    reason,
                    reservations.Where(r => r.Status == PaymentStatus.Success).Sum(r => r.Amount),
                    reservations.Where(r => r.Status != PaymentStatus.Success).Sum(r => r.Amount),
                    voidedDues.Count,
                    voidedDues.Sum()
                );

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await NotifySeatsReleasedAsync(orderId, releasedSeats);

        var gatewayStatuses = await ExecuteGatewayRefundsAsync(orderId, reservations);

        var refunds = reservations
            .Select(r => new OrderCancellationRefund(
                r.RefundTransactionId,
                r.Method,
                r.Amount,
                gatewayStatuses.GetValueOrDefault(r.RefundTransactionId, r.Status)
            ))
            .ToList();
        var totalRefunded = refunds.Where(r => r.Status == PaymentStatus.Success).Sum(r => r.Amount);

        logger.LogInformation(
            "OrderCancelled(Confirmed) OrderId {OrderId} ActorKind {Kind} ActorUserId {UserId} Refunded {Refunded} Pending {Pending}",
            orderId,
            actor.Kind,
            actor.UserId,
            totalRefunded,
            refunds.Count(r => r.Status != PaymentStatus.Success)
        );
        return new OrderCancellationResult(
            orderId,
            orderCode,
            OrderStatus.Cancelled,
            totalRefunded,
            refunds
        );
    }

    private async Task NotifySeatsReleasedAsync(
        int orderId,
        IReadOnlyList<ReleasedSeat> releasedSeats
    )
    {
        try
        {
            foreach (var group in releasedSeats.GroupBy(s => s.ShowtimeId))
                await realtimeNotifier.NotifySeatReleasedAsync(
                    group.Key,
                    [.. group.Select(s => s.ShowtimeSeatId)],
                    CancellationToken.None
                );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OrderCancelSeatNotifyFailed OrderId {OrderId}", orderId);
        }
    }

    private async Task<Dictionary<int, PaymentStatus>> ExecuteGatewayRefundsAsync(
        int orderId,
        IReadOnlyList<OrderRefundReservation> reservations
    )
    {
        var pending = reservations
            .Where(r => r.Method.IsGatewayMethod() && r.Status == PaymentStatus.Pending)
            .ToList();
        if (pending.Count == 0)
            return new Dictionary<int, PaymentStatus>();

        try
        {
            var results = await orderRefunds.ExecuteAsync(pending, false, CancellationToken.None);
            return results.ToDictionary(r => r.RefundTransactionId, r => r.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "OrderRefundExecuteAfterCancelFailed OrderId {OrderId} RefundTransactionIds {RefundIds}",
                orderId,
                string.Join(",", pending.Select(r => r.RefundTransactionId))
            );
            return new Dictionary<int, PaymentStatus>();
        }
    }

    private async Task EnsureCancellableAsync(
        int orderId,
        CancelActor actor,
        CancelReason reason,
        CinemaSettingsSnapshot settings,
        CancellationToken cancellationToken
    )
    {
        if (
            await db.OrderExchanges.AnyAsync(
                e => e.OldOrderLine.OrderId == orderId,
                cancellationToken
            )
            && !await db
                .OrderLines.AsNoTracking()
                .ExcludingExchangedOut(db)
                .AnyAsync(
                    l =>
                        l.OrderId == orderId
                        && (l.LineType == OrderLineType.Ticket || l.LineType == OrderLineType.Combo)
                        && l.Quantity > l.RefundedQuantity,
                    cancellationToken
                )
        )
            throw new BusinessRuleException(Messages.Rule_CancelOrderAllLinesExchanged);

        if (
            await db.OrderLines.AnyAsync(
                l => l.OrderId == orderId && l.IsCheckedIn,
                cancellationToken
            )
        )
            throw new BusinessRuleException(Messages.Rule_BookingHasCheckedInTicket);

        if (actor.IsCustomer)
        {
            var methods = await db
                .PaymentTransactions.AsNoTracking()
                .Where(t =>
                    t.OrderId == orderId
                    && t.Type == PaymentTransactionType.Charge
                    && t.Status == PaymentStatus.Success
                )
                .Select(t => t.Method)
                .Distinct()
                .ToListAsync(cancellationToken);
            if (!methods.All(OrderCancellationRules.IsCustomerRefundableMethod))
                throw new BusinessRuleException(Messages.Rule_BookingCancelAtCounter);
        }

        if (reason == CancelReason.RoomClosed)
        {
            await EnsureRoomChangeCancellableAsync(orderId, cancellationToken);
            return;
        }

        var showtimeStart = await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l => l.OrderId == orderId && l.LineType == OrderLineType.Ticket)
            .Select(l => (DateTime?)l.Showtime!.StartTime)
            .MinAsync(cancellationToken);
        OrderCancellationRules.EnsureRefundEligible(
            settings.CancelRefundPercent,
            settings.CancelDeadlineHours,
            showtimeStart,
            DateTime.UtcNow
        );
    }

    private async Task EnsureRoomChangeCancellableAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var changedShowtimes = await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.OrderId == orderId
                && l.LineType == OrderLineType.Ticket
                && l.ShowtimeId != null
                && db.ShowtimeRoomChanges.Any(c => c.ShowtimeId == l.ShowtimeId)
            )
            .Select(l => new { l.Showtime!.Status, l.Showtime.StartTime })
            .ToListAsync(cancellationToken);

        OrderCancellationRules.EnsureRoomChangeCancellable(
            [.. changedShowtimes.Select(s => (s.Status, s.StartTime))],
            DateTime.UtcNow
        );
    }

    private static bool ShouldAudit(CancelActor actor, CancelReason reason) =>
        !actor.IsCustomer || reason == CancelReason.RoomClosed;

    private void RecordAudit(
        CancelActor actor,
        int orderId,
        OrderStatus before,
        CancelReason reason,
        decimal refunded,
        decimal manualRefund,
        int voidedRefundDueCount = 0,
        decimal voidedRefundDueAmount = 0m
    ) =>
        auditLogger.Record(
            AuditActions.Cancel,
            AuditEntityTypes.Order,
            orderId.ToString(),
            AuditSnapshots.OrderBeforeCancel(before),
            AuditSnapshots.OrderAfterCancel(
                actor,
                reason,
                refunded,
                manualRefund,
                voidedRefundDueCount,
                voidedRefundDueAmount
            )
        );
}
