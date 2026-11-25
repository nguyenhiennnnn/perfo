using Cinema.Application.DTOs.GiftCards;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderGiftCardService
{
    Task<OrderGiftCardResponse> ApplyAsync(
        int orderId,
        OrderPromotionCaller caller,
        string code,
        CancellationToken cancellationToken
    );

    Task<OrderGiftCardResponse> RemoveAsync(
        int orderId,
        OrderPromotionCaller caller,
        int giftCardId,
        CancellationToken cancellationToken
    );
}
