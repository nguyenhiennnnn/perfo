using Cinema.Application.DTOs.Showtimes;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IShowtimeEarlyAccessService
{
    Task<ShowtimeEarlyAccessSettingDto> GetAsync(
        int showtimeId,
        CancellationToken cancellationToken
    );

    Task<ShowtimeEarlyAccessSettingDto> UpsertAsync(
        int showtimeId,
        UpsertShowtimeEarlyAccessRequest request,
        CancellationToken cancellationToken
    );

    Task<int> RemoveAsync(int showtimeId, CancellationToken cancellationToken);

    Task EnsureCanHoldAsync(int showtimeId, int? userId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, ShowtimeEarlyAccessDto>> GetForViewerAsync(
        IReadOnlyCollection<int> showtimeIds,
        int? viewerUserId,
        CancellationToken cancellationToken
    );
}
