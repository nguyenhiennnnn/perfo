using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalInsightService
{
    Task<List<RoomRentalCalendarItemDto>> GetCalendarAsync(
        DateOnly from,
        DateOnly to,
        int? roomId,
        CancellationToken cancellationToken
    );

    Task<RoomRentalCancelPreviewDto> GetCancelPreviewAsync(
        int requestId,
        bool venueIncident,
        CancellationToken cancellationToken
    );

    Task<RoomRentalReportDto> GetReportAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    );
}
