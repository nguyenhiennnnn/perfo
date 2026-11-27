using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Memberships;

namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipSubscriptionQueryService
{
    Task<IReadOnlyList<MembershipSubscriptionDto>> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    );

    Task<OffsetPageResult<AdminMembershipSubscriptionDto>> GetAdminAsync(
        int page,
        int pageSize,
        MembershipSubscriptionListFilter filter,
        CancellationToken cancellationToken
    );
}
