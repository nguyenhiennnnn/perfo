using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderReleaseService
{
    Task<bool> ExpireAsync(int orderId, CancellationToken cancellationToken);

    Task<bool> CancelPendingAsync(
        int orderId,
        CancelReason reason,
        CancelledBy cancelledBy,
        CancellationToken cancellationToken
    );
}
