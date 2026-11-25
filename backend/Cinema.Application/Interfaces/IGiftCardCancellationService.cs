using Cinema.Application.DTOs.GiftCards;

namespace Cinema.Application.Interfaces;

public interface IGiftCardCancellationService
{
    Task<CancelGiftCardResponse> CancelAsync(
        int giftCardId,
        CancelGiftCardRequest request,
        CancellationToken cancellationToken
    );
}
