using Cinema.Application.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Orders;

public class OrderConfirmedEmailHandler(
    IAppDbContext db,
    INotificationService notificationService,
    IOrderComboSuggestionService comboSuggestions,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<OrderConfirmedEmailHandler> logger
) : IBackgroundEventHandler<OrderConfirmed>
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
                UserId = o.UserId!.Value,
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
            tickets,
            await LoadComboSuggestionsAsync(
                order.UserId,
                businessEvent.OrderId,
                cancellationToken
            )
        );
    }

    private async Task<BookingComboSuggestionEmailData?> LoadComboSuggestionsAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var suggestions = await comboSuggestions.GetAsync(userId, orderId, cancellationToken);
            if (!suggestions.CanSupplement || suggestions.ValidUntil is not { } validUntil)
                return null;
            if (suggestions.Items.Count == 0)
                return null;

            return new BookingComboSuggestionEmailData(
                suggestions.DiscountPercent,
                validUntil,
                $"{appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/')}/account/tickets/{orderId}",
                [
                    .. suggestions.Items.Select(i => new BookingComboSuggestionLine(
                        i.Name,
                        i.SuggestedQuantity,
                        i.ListPrice,
                        i.PreorderPrice
                    )),
                ]
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "BookingComboSuggestionFailed OrderId {OrderId}", orderId);
            return null;
        }
    }
}
