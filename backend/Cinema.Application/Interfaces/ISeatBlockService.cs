using Cinema.Application.DTOs.Showtimes;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

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
