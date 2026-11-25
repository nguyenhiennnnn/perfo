namespace Cinema.Application.Interfaces.Recommendations;

public interface IMovieIntroEmailService
{
    Task<int> SendDueAsync(CancellationToken cancellationToken);
}
