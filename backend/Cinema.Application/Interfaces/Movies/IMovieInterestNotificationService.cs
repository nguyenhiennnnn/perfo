namespace Cinema.Application.Interfaces.Movies;

public interface IMovieInterestNotificationService
{
    Task<int> NotifyOpenedAsync(int movieId, CancellationToken cancellationToken);
}
