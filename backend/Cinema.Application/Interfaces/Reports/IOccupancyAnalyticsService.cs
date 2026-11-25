using Cinema.Application.DTOs.Dashboard;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Reports;

public interface IOccupancyAnalyticsService
{
    Task<OccupancyAnalyticsDto> GetAsync(
        DateOnly from,
        DateOnly to,
        OccupancyGroupBy groupBy,
        IReadOnlyCollection<ShowtimeKind>? kinds,
        CancellationToken cancellationToken
    );
}
