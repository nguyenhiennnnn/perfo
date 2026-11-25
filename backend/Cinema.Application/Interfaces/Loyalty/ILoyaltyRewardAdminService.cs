using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Loyalty;

namespace Cinema.Application.Interfaces.Loyalty;

public interface ILoyaltyRewardAdminService
{
    Task<OffsetPageResult<LoyaltyRewardDto>> GetAsync(
        int page,
        int pageSize,
        LoyaltyRewardListFilter filter,
        CancellationToken cancellationToken
    );

    Task<LoyaltyRewardDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<LoyaltyRewardDto> CreateAsync(
        UpsertLoyaltyRewardRequest request,
        CancellationToken cancellationToken
    );

    Task<LoyaltyRewardDto> UpdateAsync(
        int id,
        UpsertLoyaltyRewardRequest request,
        CancellationToken cancellationToken
    );

    Task<DeleteLoyaltyRewardResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}

public interface ILoyaltyRewardCatalogService
{
    Task<IReadOnlyList<LoyaltyRewardCatalogItemDto>> GetRedeemableAsync(
        CancellationToken cancellationToken
    );
}

public interface ILoyaltyRewardRedemptionService
{
    Task<RedeemLoyaltyRewardResponse> RedeemAsync(
        int userId,
        int rewardId,
        CancellationToken cancellationToken
    );
}
