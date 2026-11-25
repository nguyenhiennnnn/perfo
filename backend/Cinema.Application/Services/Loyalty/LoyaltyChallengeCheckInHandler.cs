using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Loyalty;

namespace Cinema.Application.Services.Loyalty;

public class LoyaltyChallengeCheckInHandler(ILoyaltyChallengeProgressService progress)
    : IBackgroundEventHandler<TicketCheckedIn>
{
    public Task HandleAsync(TicketCheckedIn businessEvent, CancellationToken cancellationToken) =>
        progress.RecordCheckInAsync(businessEvent, cancellationToken);
}
