using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public interface IGiftCardPurchaseService
{
    Task<HoldOrderResponse> PurchaseAsync(
        int userId,
        PurchaseGiftCardRequest request,
        CancellationToken cancellationToken
    );
}
