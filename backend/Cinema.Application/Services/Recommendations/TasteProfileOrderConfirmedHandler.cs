using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Recommendations;

namespace Cinema.Application.Services.Recommendations;

public class TasteProfileOrderConfirmedHandler(ICustomerTasteProfileService profiles)
    : IBackgroundEventHandler<OrderConfirmed>
{
    public Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken) =>
        profiles.RebuildForOrderAsync(businessEvent.OrderId, cancellationToken);
}
