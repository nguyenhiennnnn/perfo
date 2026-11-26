using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalTrackingService
{
    Task<RoomRentalTrackingDto> GetByTokenAsync(string token, CancellationToken cancellationToken);
}
