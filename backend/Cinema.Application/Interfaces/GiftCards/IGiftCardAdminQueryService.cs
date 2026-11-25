using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.GiftCards;

namespace Cinema.Application.Interfaces.GiftCards;

public interface IGiftCardAdminQueryService
{
    Task<OffsetPageResult<AdminGiftCardListItemDto>> GetAsync(
        AdminGiftCardListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<List<AdminGiftCardLedgerEntryDto>> GetHistoryAsync(
        int giftCardId,
        CancellationToken cancellationToken
    );
}
