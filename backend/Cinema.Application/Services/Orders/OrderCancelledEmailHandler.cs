using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderCancelledEmailHandler(
    IAppDbContext db,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService
) : IBackgroundEventHandler<OrderCancelled>
{
    public async Task HandleAsync(OrderCancelled businessEvent, CancellationToken cancellationToken)
    {
        if (businessEvent.Reason is CancelReason.ShowtimeCancelled or CancelReason.RoomClosed)
            return;

        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == businessEvent.OrderId && o.UserId != null)
            .Select(o => new
            {
                o.OrderCode,
                o.Type,
                o.CustomerName,
                UserId = o.UserId!.Value,
                o.User!.Email,
                o.User.FullName,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null || string.IsNullOrWhiteSpace(order.Email))
            return;

        if (!await preferences.CanSendAsync(order.UserId, EmailCategory.Service, cancellationToken))
            return;

        var refunds = await db
            .PaymentTransactions.AsNoTracking()
            .Where(t =>
                t.OrderId == businessEvent.OrderId
                && t.Type == PaymentTransactionType.Refund
                && t.Status != PaymentStatus.Failed
            )
            .GroupBy(t => t.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        if (order.Type == OrderType.Supplement && refunds.Count == 0)
            return;

        var tickets = await OrderTicketEmailLineLoader.LoadAsync(
            db,
            businessEvent.OrderId,
            cancellationToken
        );

        notificationService.SendOrderCancelled(
            order.Email,
            order.CustomerName ?? order.FullName,
            order.OrderCode,
            tickets,
            [
                .. refunds
                    .OrderBy(r => r.Method)
                    .Select(r => new OrderRefundEmailLine(r.Method, r.Amount)),
            ]
        );
    }
}
