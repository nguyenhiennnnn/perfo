using Cinema.Application.DTOs.Dashboard;

namespace Cinema.Application.Interfaces;

public interface ICancellationAnalyticsService
{
    Task<CancellationAnalyticsDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    );
}
