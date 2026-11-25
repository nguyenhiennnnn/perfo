using Cinema.Application.DTOs.Showtimes;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IShowtimeRoomChangeService
{
    Task<RoomChangePreviewResponse> PreviewAsync(
        int showtimeId,
        ShowtimeRoomChangeRequest request,
        CancellationToken cancellationToken
    );

    Task<ShowtimeRoomChangeResponse> ApplyAsync(
        int showtimeId,
        ShowtimeRoomChangeRequest request,
        CancellationToken cancellationToken
    );
}
