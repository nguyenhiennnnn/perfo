using Cinema.Application.DTOs.Showtimes;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IShowtimeEventService
{
    Task<ShowtimeEventDto> GetAsync(int showtimeId, CancellationToken cancellationToken);

    Task<ShowtimeEventDto> UpsertAsync(
        int showtimeId,
        UpsertShowtimeEventRequest request,
        CancellationToken cancellationToken
    );

    Task<int> RemoveAsync(int showtimeId, CancellationToken cancellationToken);
}
