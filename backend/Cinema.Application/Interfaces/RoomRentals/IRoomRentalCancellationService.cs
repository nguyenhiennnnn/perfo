using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalCancellationService
{
    Task<RoomRentalCancellationResultDto> CancelAsync(
        int requestId,
        RoomRentalCancelKind kind,
        string? note,
        CancellationToken cancellationToken
    );

    Task<RoomRentalCancellationResultDto?> TryCancelOverdueAsync(
        int requestId,
        CancellationToken cancellationToken
    );
}

public interface IRoomRentalBalanceOverdueService
{
    Task<int> CancelOverdueAsync(CancellationToken cancellationToken);
}
