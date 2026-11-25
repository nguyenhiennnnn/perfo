using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Orders;

namespace Cinema.Application.Services.Orders;

public class OrderConfirmedEmailHandler(IOrderTicketEmailSender ticketEmailSender)
    : IBackgroundEventHandler<OrderConfirmed>
{
    public async Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken) =>
        await ticketEmailSender.SendAsync(businessEvent.OrderId, cancellationToken);
}
