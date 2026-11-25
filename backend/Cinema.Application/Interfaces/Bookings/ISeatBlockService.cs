using Cinema.Application.DTOs.Showtimes;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Bookings;

public interface ISeatBlockService
{
    Task<List<SeatBlockDto>> GetByShowtimeAsync(int showtimeId, CancellationToken cancellationToken);

    Task<List<SeatBlockDto>> BlockAsync(
        int showtimeId,
        BlockSeatsRequest request,
        CancellationToken cancellationToken
    );

    Task<UnblockSeatsResponse> UnblockAsync(
        int showtimeId,
        UnblockSeatsRequest request,
        CancellationToken cancellationToken
    );

    Task<List<SeatBlockDto>> BlockSeatsAsync(
        int showtimeId,
        IReadOnlyCollection<int> showtimeSeatIds,
        SeatBlockType type,
        string reason,
        DateTime? expiresAt,
        CancellationToken cancellationToken,
        int? waitlistEntryId = null
    );

    Task<List<int>> ReleaseWaitlistReserveAsync(
        int showtimeId,
        IReadOnlyCollection<int> waitlistEntryIds,
        CancellationToken cancellationToken
    );

    Task<int> UnblockSeatsAsync(
        int showtimeId,
        IReadOnlyCollection<int> showtimeSeatIds,
        IReadOnlyCollection<SeatBlockType>? allowedTypes,
        CancellationToken cancellationToken
    );

    Task<int> ReleaseExpiredAsync(CancellationToken cancellationToken);
}
