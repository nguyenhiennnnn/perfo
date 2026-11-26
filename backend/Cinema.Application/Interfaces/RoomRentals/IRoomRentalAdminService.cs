using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalAdminService
{
    Task<OffsetPageResult<RoomRentalAdminListItemDto>> GetAsync(
        RoomRentalAdminListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<RoomRentalAdminDto> GetByIdAsync(int requestId, CancellationToken cancellationToken);

    Task<RoomRentalAdminDto> ChangeStatusAsync(
        int requestId,
        ChangeRoomRentalRequestStatusRequest request,
        CancellationToken cancellationToken
    );
}
