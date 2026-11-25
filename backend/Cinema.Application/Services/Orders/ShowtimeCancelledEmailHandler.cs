using Cinema.Application.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Users;


namespace Cinema.Application.Services.Orders;

public class ShowtimeCancelledEmailHandler(
    IAppDbContext db,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService
) : IBackgroundEventHandler<OrderCancelled>
{
    public async Task HandleAsync(OrderCancelled businessEvent, CancellationToken cancellationToken)
    {
        if (businessEvent.Reason is not (CancelReason.ShowtimeCancelled or CancelReason.RoomClosed))
            return;

        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == businessEvent.OrderId && o.UserId != null)
            .Where(ShowtimeCancellationRouting.CancelledByShowtime)
            .Select(o => new
            {
                o.OrderCode,
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

        notificationService.SendShowtimeCancelledNotice(
            order.Email,
            order.CustomerName ?? order.FullName,
            order.OrderCode,
            [
                .. refunds
                    .OrderBy(r => r.Method)
                    .Select(r => new OrderRefundEmailLine(r.Method, r.Amount)),
            ]
        );
    }
}
