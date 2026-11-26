using Cinema.Application.DTOs.Orders;
using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Bundles;

public interface IOrderBundleComposer
{
    Task ComposeAsync(
        Order order,
        int showtimeId,
        IReadOnlyList<BundleSelectionRequest>? selections,
        CancellationToken cancellationToken
    );
}
