namespace Cinema.Application.Interfaces.Events;

public interface IOutboxProcessor
{
    Task<int> ProcessDueAsync(CancellationToken cancellationToken);
    Task<int> PurgeProcessedAsync(CancellationToken cancellationToken);
}
