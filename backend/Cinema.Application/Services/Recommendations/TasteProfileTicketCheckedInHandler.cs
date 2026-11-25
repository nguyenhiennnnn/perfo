using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Recommendations;

namespace Cinema.Application.Services.Recommendations;

public class TasteProfileTicketCheckedInHandler(ICustomerTasteProfileService profiles)
    : IBackgroundEventHandler<TicketCheckedIn>
{
    public Task HandleAsync(TicketCheckedIn businessEvent, CancellationToken cancellationToken) =>
        profiles.RebuildForOrderLineAsync(businessEvent.OrderLineId, cancellationToken);
}
