namespace Cinema.Application.Interfaces.Recommendations;

public interface IMovieAffinityService
{
    Task RefreshAsync(CancellationToken cancellationToken);
}
