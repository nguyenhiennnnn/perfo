using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public interface IUnclaimedComboOrderService
{
    Task<OffsetPageResult<UnclaimedComboOrderDto>> GetAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken
    );
}
