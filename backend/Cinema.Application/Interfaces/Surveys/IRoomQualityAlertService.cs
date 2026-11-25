namespace Cinema.Application.Interfaces.Surveys;

public interface IRoomQualityAlertService
{
    Task<int> RunAsync(CancellationToken cancellationToken);
}
