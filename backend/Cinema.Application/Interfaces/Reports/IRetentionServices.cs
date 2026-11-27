using Cinema.Application.DTOs.Reports;

namespace Cinema.Application.Interfaces.Reports;

public interface IRetentionReportService
{
    Task<RetentionReportDto> GetAsync(int months, CancellationToken cancellationToken);
}

public interface IRetentionStatsService
{
    Task RefreshAsync(CancellationToken cancellationToken);
}
