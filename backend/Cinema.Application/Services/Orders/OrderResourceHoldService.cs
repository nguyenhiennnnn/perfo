using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Orders;

public class OrderResourceHoldService(IAppDbContext db, IEnumerable<IResourceHold> holds)
    : IOrderResourceHoldService
{
    private readonly IReadOnlyList<IResourceHold> _holds = holds.ToList();

    public Task ReserveAllAsync(Order order, CancellationToken cancellationToken) =>
        RunAsync(
            hold => hold.ReserveAsync(new ResourceHoldRequest(order), cancellationToken),
            cancellationToken
        );

    public Task CommitAllAsync(int orderId, CancellationToken cancellationToken) =>
        RunAsync(hold => hold.CommitAsync(orderId, cancellationToken), cancellationToken);

    public Task ReleaseAllAsync(int orderId, CancellationToken cancellationToken) =>
        RunAsync(hold => hold.ReleaseAsync(orderId, cancellationToken), cancellationToken);

    private async Task RunAsync(Func<IResourceHold, Task> action, CancellationToken cancellationToken)
    {
        if (_holds.Count == 0)
            return;

        await db.InTransactionAsync(
            async () =>
            {
                foreach (var hold in _holds)
                    await action(hold);
            },
            cancellationToken
        );
    }
}
