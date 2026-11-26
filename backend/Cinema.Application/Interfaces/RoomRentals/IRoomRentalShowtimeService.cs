using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalShowtimeService
{
    Task<RoomRentalShowtimeDto> EnsureForRequestAsync(
        int requestId,
        CancellationToken cancellationToken
    );
}
