using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Reports.Finance;

public class MovieProfitBlock : IReportBlock
{
    public string Key => "movie-profit";
    public string Title => Messages.Report_Finance_MovieProfit_Title;
    public int Order => 60;

    public Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();
}
