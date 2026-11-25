using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Recommendations;

namespace Cinema.Application.Services.Recommendations;

public class TasteProfileReviewChangedHandler(ICustomerTasteProfileService profiles)
    : IBackgroundEventHandler<ReviewChanged>
{
    public Task HandleAsync(ReviewChanged businessEvent, CancellationToken cancellationToken) =>
        profiles.RebuildAsync(businessEvent.UserId, cancellationToken);
}
