using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalTrackingQuoteService
{
    Task<RoomRentalTrackingQuotesDto> ListAsync(string token, CancellationToken cancellationToken);

    Task<ExportFileDto> ExportPdfAsync(
        string token,
        int version,
        CancellationToken cancellationToken
    );

    Task<RoomRentalTrackingQuoteDecisionDto> AcceptAsync(
        string token,
        int version,
        CancellationToken cancellationToken
    );

    Task<RoomRentalTrackingQuoteDecisionDto> DeclineAsync(
        string token,
        int version,
        string? reason,
        CancellationToken cancellationToken
    );
}

public interface IRoomRentalQuoteExpiryService
{
    Task<int> ExpireDueAsync(CancellationToken cancellationToken);
}
