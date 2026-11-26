using Cinema.Application.Events;
using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Services.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderReleaseService(
    IAppDbContext db,
    IOrderResourceHoldService resourceHolds,
    IBusinessEventDispatcher eventDispatcher,
    IRealtimeNotifier realtimeNotifier,
    IAbuseSignalService abuseSignals,
    ILogger<OrderReleaseService> logger
) : IOrderReleaseService
{
    public Task<bool> ExpireAsync(int orderId, CancellationToken cancellationToken) =>
        ReleasePendingAsync(orderId, OrderStatus.Expired, null, null, cancellationToken);

    public Task<bool> CancelPendingAsync(
        int orderId,
        CancelReason reason,
        CancelledBy cancelledBy,
        CancellationToken cancellationToken
    ) => ReleasePendingAsync(orderId, OrderStatus.Cancelled, reason, cancelledBy, cancellationToken);

    private async Task<bool> ReleasePendingAsync(
        int orderId,
        OrderStatus target,
        CancelReason? reason,
        CancelledBy? cancelledBy,
        CancellationToken cancellationToken
    )
    {
        var releasedSeats = new List<(int ShowtimeId, int SeatId)>();
        var released = false;

        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var now = DateTime.UtcNow;
                DateTime? cancelledAt = target == OrderStatus.Cancelled ? now : null;
                var claimed = await db
                    .Orders.Where(o => o.OrderId == orderId && o.Status == OrderStatus.Pending)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters
                                .SetProperty(o => o.Status, target)
                                .SetProperty(o => o.CancelReason, reason)
                                .SetProperty(o => o.CancelledBy, cancelledBy)
                                .SetProperty(o => o.CancelledAt, cancelledAt),
                        cancellationToken
                    );
                if (claimed == 0)
                    return;

                if (target == OrderStatus.Expired || cancelledBy == CancelledBy.Customer)
                {
                    var abandonedBy = await db
                        .Orders.AsNoTracking()
                        .Where(o =>
                            o.OrderId == orderId
                            && o.Channel == SalesChannel.Online
                            && o.Type == OrderType.Ticket
                            && o.UserId != null
                            && !o.PaymentTransactions.Any(t =>
                                t.Type == PaymentTransactionType.Charge
                                && t.Status == PaymentStatus.Success
                            )
                        )
                        .Select(o => o.UserId)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (abandonedBy is { } abandonedUserId)
                        await abuseSignals.RecordHoldAbandonedAsync(
                            abandonedUserId,
                            orderId,
                            cancellationToken
                        );
                }

                var seats = await db
                    .ShowtimeSeats.HeldFor(orderId)
                    .Select(ss => new { ss.ShowtimeId, ss.ShowtimeSeatId })
                    .ToListAsync(cancellationToken);
                if (seats.Count > 0)
                {
                    var seatIds = seats.Select(s => s.ShowtimeSeatId).ToList();
                    await db
                        .ShowtimeSeats.Where(ss =>
                            seatIds.Contains(ss.ShowtimeSeatId) && ss.HeldByOrderId == orderId
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters
                                    .SetProperty(x => x.Status, ShowtimeSeatStatus.Available)
                                    .SetProperty(x => x.HeldByUserId, (int?)null)
                                    .SetProperty(x => x.HeldByOrderId, (int?)null)
                                    .SetProperty(x => x.HeldUntil, (DateTime?)null),
                            cancellationToken
                        );
                    releasedSeats.AddRange(seats.Select(s => (s.ShowtimeId, s.ShowtimeSeatId)));
                    await SeatHoldEventRecorder.StageAsync(
                        db,
                        seatIds,
                        target == OrderStatus.Expired
                            ? SeatHoldEventType.Expired
                            : SeatHoldEventType.Released,
                        now,
                        cancellationToken
                    );
                    await db.SaveChangesAsync(cancellationToken);
                    foreach (var showtimeId in seats.Select(s => s.ShowtimeId).Distinct())
                        await eventDispatcher.PublishAsync(
                            new ShowtimeSeatsReleased(showtimeId, now),
                            cancellationToken
                        );
                }

                await db
                    .PaymentTransactions.Where(t =>
                        t.OrderId == orderId
                        && t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Pending
                    )
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(t => t.Status, PaymentStatus.Failed),
                        cancellationToken
                    );

                await resourceHolds.ReleaseAllAsync(orderId, cancellationToken);

                if (target == OrderStatus.Cancelled)
                    await eventDispatcher.PublishAsync(
                        new OrderCancelled(orderId, reason, cancelledBy, now),
                        cancellationToken
                    );

                released = true;
            },
            cancellationToken
        );

        if (!released)
            return false;

        foreach (var group in releasedSeats.GroupBy(s => s.ShowtimeId))
        {
            var seatIds = group.Select(s => s.SeatId).ToList();
            if (target == OrderStatus.Expired)
                await realtimeNotifier.NotifyHoldExpiredAsync(
                    group.Key,
                    orderId,
                    seatIds,
                    cancellationToken
                );
            else
                await realtimeNotifier.NotifySeatReleasedAsync(group.Key, seatIds, cancellationToken);
        }

        logger.LogInformation(
            "OrderReleased OrderId {OrderId} Status {Status} SeatsReleased {Count}",
            orderId,
            target,
            releasedSeats.Count
        );
        return true;
    }
}
