using Cinema.Application.DTOs.Showtimes;

namespace Cinema.Application.Interfaces.Showtimes;

public interface IShowtimeAutoScheduleService
{
    Task<AutoScheduleResponseDto> PreviewAutoScheduleAsync(
        AutoScheduleRequest request,
        CancellationToken cancellationToken
    );
    Task<BulkCreateShowtimesResponse> ApplyAutoScheduleAsync(
        ApplyAutoScheduleRequest request,
        CancellationToken cancellationToken
    );
}
