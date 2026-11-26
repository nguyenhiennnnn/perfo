using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalRequestService
{
    Task<RoomRentalRequestReceiptDto> CreatePublicAsync(
        CreateRoomRentalRequestRequest request,
        CancellationToken cancellationToken
    );
}
