using Cinema.Application.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Cinema.Application.Interfaces.Events;


namespace Cinema.Application.Services.Orders;

public class OrderExchangedEmailHandler(IAppDbContext db, INotificationService notificationService)
    : IBackgroundEventHandler<OrderExchanged>
{
    public async Task HandleAsync(OrderExchanged businessEvent, CancellationToken cancellationToken)
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
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null || string.IsNullOrWhiteSpace(order.Email))
            return;

        var exchange = await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == businessEvent.ExchangeOrderId)
            .Select(o => new { o.OrderCode, o.FinalAmount })
            .FirstOrDefaultAsync(cancellationToken);
        var showtime = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.ShowtimeId == businessEvent.NewShowtimeId)
            .Select(s => new
            {
                MovieTitle = s.Movie.Title,
                s.StartTime,
                s.Room.RoomName,
            })
            .FirstOrDefaultAsync(cancellationToken);
        var oldStart = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.ShowtimeId == businessEvent.OldShowtimeId)
            .Select(s => (DateTime?)s.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
        if (exchange is null || showtime is null || oldStart is null)
            return;

        var tickets = await OrderTicketEmailLineLoader.LoadAsync(
            db,
            businessEvent.ExchangeOrderId,
            cancellationToken
        );
        if (tickets.Count == 0)
            return;

        var credit = await db
            .GiftCardLedgers.AsNoTracking()
            .Where(l =>
                l.OrderId == businessEvent.ExchangeOrderId
                && l.EntryType == GiftCardLedgerType.Issue
                && l.GiftCard.Type == GiftCardType.ExchangeCredit
            )
            .Select(l => new
            {
                l.GiftCard.Code,
                l.GiftCard.FaceValue,
                l.GiftCard.ExpiresAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        notificationService.SendOrderExchanged(
            order.Email,
            new OrderExchangeEmailData(
                order.CustomerName ?? order.FullName,
                order.OrderCode,
                exchange.OrderCode,
                showtime.MovieTitle,
                oldStart.Value,
                showtime.StartTime,
                showtime.RoomName,
                tickets,
                exchange.FinalAmount,
                credit?.FaceValue ?? 0m,
                credit?.Code,
                credit?.ExpiresAt
            )
        );
    }
}
