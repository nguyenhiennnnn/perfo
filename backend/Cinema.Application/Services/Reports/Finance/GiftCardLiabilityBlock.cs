using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Reports.Finance;

public class GiftCardLiabilityBlock : IReportBlock
{
    public string Key => "gift-card-liability";
    public string Title => Messages.Report_Finance_GiftCardLiability_Title;
    public int Order => 70;

    public Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();
}
