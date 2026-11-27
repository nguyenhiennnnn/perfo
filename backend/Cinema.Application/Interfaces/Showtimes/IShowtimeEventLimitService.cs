using Cinema.Application.DTOs.Showtimes;
using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IShowtimeEventLimitService
{
    Task<ShowtimeEventLimitsDto> GetAsync(int showtimeId, CancellationToken cancellationToken);

    Task<ShowtimeEventLimitsDto> UpsertAsync(
        int showtimeId,
        UpsertShowtimeEventLimitsRequest request,
        CancellationToken cancellationToken
    );

    Task EnsureWithinLimitsAsync(Order order, CancellationToken cancellationToken);
}
