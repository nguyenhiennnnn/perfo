using Cinema.Application.DTOs.Memberships;

namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipFreeTicketQueryService
{
    Task<MembershipFreeTicketBalanceDto> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    );
}
