using Cinema.Application.DTOs.Memberships;

namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipSubscriptionQueryService
{
    Task<IReadOnlyList<MembershipSubscriptionDto>> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    );
}
