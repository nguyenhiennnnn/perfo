using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.GiftCards;

namespace Cinema.Application.Interfaces;

public interface IGiftCardPurchasedQueryService
{
    Task<CursorPageResult<PurchasedGiftCardDto>> GetPurchasedAsync(
        int userId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken
    );
}
