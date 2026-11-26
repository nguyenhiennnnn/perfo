using Cinema.Application.DTOs.Abuse;
using Cinema.Application.DTOs.Common;

namespace Cinema.Application.Interfaces.Abuse;

public interface ICustomerRestrictionService
{
    Task<ActiveCustomerRestriction?> SyncAsync(int userId, CancellationToken cancellationToken);

    Task<OffsetPageResult<CustomerRestrictionDto>> GetAdminAsync(
        CustomerRestrictionListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<CustomerRestrictionDto> LiftAsync(int id, CancellationToken cancellationToken);
}
