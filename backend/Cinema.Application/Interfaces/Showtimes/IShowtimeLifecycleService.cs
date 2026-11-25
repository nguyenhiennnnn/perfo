namespace Cinema.Application.Interfaces.Showtimes;

public interface IShowtimeLifecycleService
{
    Task<int> CompleteEndedShowtimesAsync(CancellationToken cancellationToken);
    Task<int> EndMoviesWithoutUpcomingShowtimesAsync(CancellationToken cancellationToken);
}
