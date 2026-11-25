using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderCancellationFinalizer(
    IAppDbContext db,
    IEnumerable<IOrderCancellationHook> hooks,
    IBusinessEventDispatcher eventDispatcher
) : IOrderCancellationFinalizer
{
    public async Task<IReadOnlyList<ReleasedSeat>> FinalizeAsync(
        int orderId,
        CancelReason reason,
        CancelledBy cancelledBy,
        OrderRefundPlan refundPlan,
        CancellationToken cancellationToken
    )
    {
        var order =
            await db
                .Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        var now = DateTime.UtcNow;
        var claimed = await db
            .Orders.Where(o => o.OrderId == orderId && o.Status == OrderStatus.Confirmed)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(o => o.Status, OrderStatus.Cancelled)
                        .SetProperty(o => o.CancelReason, (CancelReason?)reason)
                        .SetProperty(o => o.CancelledBy, (CancelledBy?)cancelledBy)
                        .SetProperty(o => o.CancelledAt, (DateTime?)now),
                cancellationToken
            );
        if (claimed == 0)
            throw new ConflictAppException(Messages.Conflict_BookingBeingProcessedOrCancelled);

        order.Status = OrderStatus.Cancelled;
        order.CancelReason = reason;
        order.CancelledBy = cancelledBy;
        order.CancelledAt = now;

        var releasedSeats = await ReleaseSeatsAsync(orderId, cancellationToken);

        var context = new OrderCancellationContext(order, reason, cancelledBy, refundPlan);
        foreach (var hook in hooks)
            await hook.OnCancelledAsync(context, cancellationToken);

        await eventDispatcher.PublishAsync(
            new OrderCancelled(orderId, reason, cancelledBy, now),
            cancellationToken
        );

        return releasedSeats;
    }

    private async Task<IReadOnlyList<ReleasedSeat>> ReleaseSeatsAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var seats = await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.OrderId == orderId
                && l.LineType == OrderLineType.Ticket
                && l.ShowtimeSeatId != null
                && l.ShowtimeSeat!.Status == ShowtimeSeatStatus.Booked
            )
            .Select(l => new { l.ShowtimeSeat!.ShowtimeId, l.ShowtimeSeat.ShowtimeSeatId })
            .ToListAsync(cancellationToken);
        if (seats.Count == 0)
            return [];

        var seatIds = seats.Select(s => s.ShowtimeSeatId).ToList();
        await db
            .ShowtimeSeats.Where(ss =>
                seatIds.Contains(ss.ShowtimeSeatId) && ss.Status == ShowtimeSeatStatus.Booked
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

        return [.. seats.Select(s => new ReleasedSeat(s.ShowtimeId, s.ShowtimeSeatId))];
    }
}
