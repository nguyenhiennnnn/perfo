using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Orders;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class SupplementOrderCancelledReminderHandler(
    IAppDbContext db,
    ISupplementReminderService reminders
) : IBackgroundEventHandler<OrderCancelled>
{
    public async Task HandleAsync(OrderCancelled businessEvent, CancellationToken cancellationToken)
    {
        if (businessEvent.Reason == CancelReason.ShowtimeCancelled)
            return;

        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == businessEvent.OrderId)
            .Select(o => new { o.Type, o.OrderId })
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null || order.Type == OrderType.Supplement)
            return;

        if (
            businessEvent.Reason == CancelReason.RoomClosed
            && await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == businessEvent.OrderId)
                .Where(ShowtimeCancellationRouting.CancelledByShowtime)
                .AnyAsync(cancellationToken)
        )
            return;

        await reminders.RemindAsync(
            businessEvent.OrderId,
            SupplementReminderReason.OriginalCancelled,
            cancellationToken
        );
    }
}

public class SupplementOrderExchangedReminderHandler(ISupplementReminderService reminders)
    : IBackgroundEventHandler<OrderExchanged>
{
    public Task HandleAsync(OrderExchanged businessEvent, CancellationToken cancellationToken) =>
        reminders.RemindAsync(
            businessEvent.OrderId,
            SupplementReminderReason.OriginalExchanged,
            cancellationToken
        );
}
