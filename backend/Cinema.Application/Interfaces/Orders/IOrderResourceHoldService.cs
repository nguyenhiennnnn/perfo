using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderResourceHoldService
{
    Task ReserveAllAsync(Order order, CancellationToken cancellationToken);

    Task CommitAllAsync(int orderId, CancellationToken cancellationToken);

    Task ReleaseAllAsync(int orderId, CancellationToken cancellationToken);
}
