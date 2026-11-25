using Cinema.Application.DTOs.Import;
using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public interface IGiftCardBulkOrderService
{
    Task<HoldOrderResponse> CreateOrderAsync(
        int userId,
        ImportApplyRequest request,
        CancellationToken cancellationToken
    );
}
