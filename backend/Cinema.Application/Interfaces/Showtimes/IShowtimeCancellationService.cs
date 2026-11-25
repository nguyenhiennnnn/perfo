using Cinema.Application.DTOs.Showtimes;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IShowtimeCancellationService
{
    Task<DeleteShowtimeResponse> DeleteShowtimeAsync(
        int showtimeId,
        CancellationToken cancellationToken
    );

    Task<DeleteShowtimeResponse> DeleteShowtimeAsync(
        int showtimeId,
        CancelReason reason,
        CancellationToken cancellationToken
    );
}
