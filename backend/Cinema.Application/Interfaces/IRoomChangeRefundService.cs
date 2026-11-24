using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Showtimes;

namespace Cinema.Application.Interfaces;

public record RoomChangeRefundListFilter(
    string? Status,
    int? ShowtimeId,
    int? OrderId,
    string? OrderCode
);

public interface IRoomChangeRefundService
{
    Task<OffsetPageResult<RoomChangeRefundListItemDto>> GetRefundDuesAsync(
        RoomChangeRefundListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<RoomChangeRefundConfirmResultDto> ConfirmAsync(
        int refundDueId,
        int confirmedByUserId,
        CancellationToken cancellationToken
    );
}
