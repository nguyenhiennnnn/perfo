using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalQuoteVersionService
{
    Task<RoomRentalQuoteVersionDto> CreateAsync(
        int requestId,
        UpsertRoomRentalQuoteVersionRequest request,
        CancellationToken cancellationToken
    );

    Task<List<RoomRentalQuoteVersionListItemDto>> ListAsync(
        int requestId,
        CancellationToken cancellationToken
    );

    Task<RoomRentalQuoteVersionDto> GetAsync(
        int requestId,
        int version,
        CancellationToken cancellationToken
    );
}

public interface IRoomRentalQuotePdfService
{
    Task<Cinema.Application.DTOs.Common.ExportFileDto> ExportAsync(
        int requestId,
        int version,
        CancellationToken cancellationToken
    );
}

public interface IRoomRentalQuoteDeliveryService
{
    Task<RoomRentalQuoteVersionDto> SendAsync(
        int requestId,
        int version,
        CancellationToken cancellationToken
    );
}
