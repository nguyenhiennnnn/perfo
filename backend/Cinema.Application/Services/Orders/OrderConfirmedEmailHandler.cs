using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderConfirmedEmailHandler(IAppDbContext db, INotificationService notificationService)
    : IBackgroundEventHandler<OrderConfirmed>
{
    public async Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken)
    {
        var order = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == businessEvent.OrderId && o.UserId != null)
            .Select(o => new
            {
                o.OrderCode,
                o.CustomerName,
                o.User!.Email,
                o.User.FullName,
                MovieTitle = o
                    .Lines.Where(l => l.LineType == OrderLineType.Ticket)
                    .Select(l => l.Showtime!.Movie.Title)
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return;

        var tickets = await OrderTicketEmailLineLoader.LoadAsync(
            db,
            businessEvent.OrderId,
            cancellationToken
        );
        if (tickets.Count == 0)
            return;

        notificationService.SendBookingConfirmed(
            order.Email,
            order.CustomerName ?? order.FullName,
            order.OrderCode,
            order.MovieTitle ?? string.Empty,
            tickets
        );
    }
}
