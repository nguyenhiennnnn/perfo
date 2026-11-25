using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Loyalty;

namespace Cinema.Application.Interfaces.Loyalty;

public interface ILoyaltyMemberQueryService
{
    Task<LoyaltyMemberSummaryDto> GetSummaryAsync(int userId, CancellationToken cancellationToken);

    Task<OffsetPageResult<LoyaltyLotDto>> GetLotsAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}
