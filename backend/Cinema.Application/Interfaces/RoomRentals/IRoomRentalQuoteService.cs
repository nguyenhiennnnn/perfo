using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalQuoteService
{
    Task<RoomRentalQuoteResultDto> CalculateAsync(
        RoomRentalQuoteRequest request,
        CancellationToken cancellationToken
    );
}
