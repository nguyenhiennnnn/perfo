using Cinema.Application.DTOs.Dashboard;

namespace Cinema.Application.Interfaces;

public interface ISeatHeatmapService
{
    Task<SeatHeatmapDto> GetAsync(
        int roomId,
        DateOnly from,
        DateOnly to,
        int? movieId,
        int? genreId,
        CancellationToken cancellationToken
    );
}
