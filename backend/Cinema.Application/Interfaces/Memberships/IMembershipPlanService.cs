using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Memberships;

namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipPlanService
{
    Task<OffsetPageResult<MembershipPlanDto>> GetAdminAsync(
        int page,
        int pageSize,
        MembershipPlanListFilter filter,
        CancellationToken cancellationToken
    );

    Task<MembershipPlanDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MembershipPlanDto>> GetActiveAsync(CancellationToken cancellationToken);

    Task<MembershipPlanDto> CreateAsync(
        UpsertMembershipPlanRequest request,
        CancellationToken cancellationToken
    );

    Task<MembershipPlanDto> UpdateAsync(
        int id,
        UpsertMembershipPlanRequest request,
        CancellationToken cancellationToken
    );

    Task<DeleteMembershipPlanResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}
