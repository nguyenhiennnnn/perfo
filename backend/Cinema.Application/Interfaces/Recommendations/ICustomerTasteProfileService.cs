namespace Cinema.Application.Interfaces.Recommendations;

public interface ICustomerTasteProfileService
{
    Task RebuildAsync(int userId, CancellationToken cancellationToken);

    Task RebuildForOrderAsync(int orderId, CancellationToken cancellationToken);

    Task RebuildForOrderLineAsync(int orderLineId, CancellationToken cancellationToken);

    Task ClearAsync(int userId, CancellationToken cancellationToken);
}
