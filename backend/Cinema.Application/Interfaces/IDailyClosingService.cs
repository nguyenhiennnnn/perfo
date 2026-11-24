using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.DailyClosings;

namespace Cinema.Application.Interfaces;

public interface IDailyClosingService
{
    Task<OffsetPageResult<DailyClosingListItemDto>> GetClosingsAsync(
        DailyClosingListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<DailyClosingDto> GetByDateAsync(DateOnly businessDate, CancellationToken cancellationToken);

    Task<DailyClosingDto> CloseAsync(
        int userId,
        DateOnly businessDate,
        CloseDailyClosingRequest request,
        CancellationToken cancellationToken
    );
}
