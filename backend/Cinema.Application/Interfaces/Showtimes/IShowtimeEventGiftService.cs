using Cinema.Application.DTOs.Showtimes;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IShowtimeEventGiftService
{
    Task<ShowtimeEventGiftsDto> GetAsync(int showtimeId, CancellationToken cancellationToken);

    Task<ShowtimeEventGiftsDto> UpsertAsync(
        int showtimeId,
        UpsertShowtimeEventGiftsRequest request,
        CancellationToken cancellationToken
    );
}
