using Cinema.Application.DTOs.Reports;

namespace Cinema.Application.Interfaces.Reports;

public interface IReportBlock
{
    string Key { get; }
    string Title { get; }
    int Order { get; }

    Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken);

    Task<ReportBlockResult> BuildAsync(ReportPeriod period, CancellationToken cancellationToken);
}
