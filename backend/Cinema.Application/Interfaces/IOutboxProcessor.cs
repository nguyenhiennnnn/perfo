namespace Cinema.Application.Interfaces;

public interface IOutboxProcessor
{
    Task<int> ProcessDueAsync(CancellationToken cancellationToken);
    Task<int> PurgeProcessedAsync(CancellationToken cancellationToken);
}
