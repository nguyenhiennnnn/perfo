using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipPurchaseService
{
    Task<HoldOrderResponse> PurchaseAsync(
        int userId,
        int planId,
        CancellationToken cancellationToken
    );
}
