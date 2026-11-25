using Cinema.Application.DTOs.Reports;

namespace Cinema.Application.Interfaces.Reports;

public interface IFinanceReportService
{
    IReadOnlyList<ReportBlockInfoDto> GetBlocks();

    Task<FinanceReportDto> GetAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<string>? blocks,
        CancellationToken cancellationToken
    );
}
