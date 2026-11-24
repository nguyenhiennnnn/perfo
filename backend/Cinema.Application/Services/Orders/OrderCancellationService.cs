using Cinema.Application.Caching;
using Cinema.Application.Interfaces;
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
                })
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        if (actor.IsCustomer && order.UserId != actor.UserId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

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

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
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
        var reservations = await orderRefunds.ReserveAsync(
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
        var results = await orderRefunds.ExecuteAsync(reservations, cancellationToken);

        var gatewayIds = reservations
            .Where(r => r.Method.IsGatewayMethod())
            .Select(r => r.RefundTransactionId)
            .ToHashSet();
        var gatewayResults = results.Where(r => gatewayIds.Contains(r.RefundTransactionId)).ToList();
        if (gatewayResults.Count > 0 && gatewayResults.All(r => !r.Success))
        {
            var message =
                gatewayResults.Select(r => r.Message).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? Messages.Refund_ExecutionError;
            logger.LogWarning(
                "OrderRefundFailed OrderId {OrderId} Message {Message}",
                orderId,
                message
            );
            throw new BusinessRuleException(string.Format(Messages.Rule_RefundFailed, message));
        }

        var gatewayRefunded = gatewayResults.Where(r => r.Success).ToList();
        IReadOnlyList<ReleasedSeat> releasedSeats;
        try
        {
            var failedIds = results.Where(r => !r.Success).Select(r => r.RefundTransactionId).ToList();
            await MarkManualRefundsAsync(orderId, reservations, failedIds, cancellationToken);

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

            var refunded = results.Where(r => r.Success).Sum(r => r.Amount);
            var manual = results.Where(r => !r.Success).Sum(r => r.Amount);
            if (ShouldAudit(actor, reason))
                RecordAudit(
                    actor,
                    orderId,
                    OrderStatus.Confirmed,
                    reason,
                    refunded,
                    manual,
                    voidedDues.Count,
                    voidedDues.Sum()
                );

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (gatewayRefunded.Count > 0)
        {
            logger.LogCritical(
                ex,
                "RefundCommitFailed OrderId {OrderId} RefundTransactionIds {RefundIds} — đã hoàn tiền ở cổng nhưng DB chưa ghi nhận.",
                orderId,
                string.Join(",", gatewayRefunded.Select(r => r.RefundTransactionId))
            );
            throw;
        }

        foreach (var group in releasedSeats.GroupBy(s => s.ShowtimeId))
            await realtimeNotifier.NotifySeatReleasedAsync(
                group.Key,
                [.. group.Select(s => s.ShowtimeSeatId)],
                cancellationToken
            );

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
            "OrderCancelled(Confirmed) OrderId {OrderId} ActorKind {Kind} ActorUserId {UserId} Refunded {Refunded} Failed {Failed}",
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

    private async Task EnsureCancellableAsync(
        int orderId,
        CancelActor actor,
        CancelReason reason,
        CinemaSettingsSnapshot settings,
        CancellationToken cancellationToken
    )
    {
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
            "OrderRefundNeedsManual OrderId {OrderId} ChargeTransactionIds {ChargeIds} — cần hoàn tiền thủ công.",
            orderId,
            string.Join(",", chargeIds)
        );
    }

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
