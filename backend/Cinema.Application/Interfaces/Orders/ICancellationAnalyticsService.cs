using Cinema.Application.DTOs.Dashboard;

namespace Cinema.Application.Interfaces.Orders;

public interface ICancellationAnalyticsService
{
    Task<CancellationAnalyticsDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    );
}
