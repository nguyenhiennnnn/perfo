using Cinema.Application.DTOs.GiftCards;

namespace Cinema.Application.Interfaces.GiftCards;

public interface IGiftCardCancellationService
{
    Task<CancelGiftCardResponse> CancelAsync(
        int giftCardId,
        CancelGiftCardRequest request,
        CancellationToken cancellationToken
    );
}
