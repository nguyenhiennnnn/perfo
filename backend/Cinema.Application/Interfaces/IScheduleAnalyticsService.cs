using Cinema.Application.DTOs.Dashboard;

namespace Cinema.Application.Interfaces;

public interface IScheduleAnalyticsService
{
    Task<ScheduleReportDto> GetReportAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken
    );
}
