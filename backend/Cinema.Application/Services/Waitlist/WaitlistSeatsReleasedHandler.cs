using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Waitlist;

namespace Cinema.Application.Services.Waitlist;

public class WaitlistSeatsReleasedHandler(IWaitlistNotificationService notificationService)
    : IBackgroundEventHandler<ShowtimeSeatsReleased>
{
    public Task HandleAsync(
        ShowtimeSeatsReleased businessEvent,
        CancellationToken cancellationToken
    ) => notificationService.NotifyHeadAsync(businessEvent.ShowtimeId, cancellationToken);
}
