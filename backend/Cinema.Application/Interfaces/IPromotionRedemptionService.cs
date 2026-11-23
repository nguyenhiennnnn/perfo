using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Promotions;

namespace Cinema.Application.Interfaces;

public interface IPromotionRedemptionService
{
    Task<OffsetPageResult<PromotionRedemptionDto>> GetAsync(
        PromotionRedemptionListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}
