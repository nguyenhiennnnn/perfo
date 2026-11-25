using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Orders;

public sealed record ResourceHoldRequest(Order Order);

public interface IResourceHold
{
    string ResourceType { get; }

    Task ReserveAsync(ResourceHoldRequest request, CancellationToken cancellationToken);

    Task CommitAsync(int orderId, CancellationToken cancellationToken);

    Task ReleaseAsync(int orderId, CancellationToken cancellationToken);
}
