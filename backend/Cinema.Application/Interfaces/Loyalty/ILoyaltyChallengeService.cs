using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Loyalty;

namespace Cinema.Application.Interfaces.Loyalty;

public interface ILoyaltyChallengeAdminService
{
    Task<OffsetPageResult<LoyaltyChallengeDto>> GetAsync(
        int page,
        int pageSize,
        LoyaltyChallengeListFilter filter,
        CancellationToken cancellationToken
    );

    Task<LoyaltyChallengeDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<LoyaltyChallengeDto> CreateAsync(
        UpsertLoyaltyChallengeRequest request,
        CancellationToken cancellationToken
    );

    Task<LoyaltyChallengeDto> UpdateAsync(
        int id,
        UpsertLoyaltyChallengeRequest request,
        CancellationToken cancellationToken
    );

    Task<DeleteLoyaltyChallengeResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}

public interface ILoyaltyChallengeQueryService
{
    Task<IReadOnlyList<MyLoyaltyChallengeDto>> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    );
}
