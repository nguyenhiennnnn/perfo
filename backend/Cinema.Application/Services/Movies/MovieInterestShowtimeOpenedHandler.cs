using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Movies;

namespace Cinema.Application.Services.Movies;

public class MovieInterestShowtimeOpenedHandler(
    IMovieInterestNotificationService notificationService
) : IBackgroundEventHandler<ShowtimeOpened>
{
    public Task HandleAsync(ShowtimeOpened businessEvent, CancellationToken cancellationToken) =>
        notificationService.NotifyOpenedAsync(businessEvent.MovieId, cancellationToken);
}
