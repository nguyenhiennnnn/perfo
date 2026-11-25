using Cinema.Application.DTOs.Dashboard;

namespace Cinema.Application.Interfaces.Combos;

public interface IComboAnalyticsService
{
    Task<ComboRevenueDto> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    );
}
