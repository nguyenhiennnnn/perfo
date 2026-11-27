using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class MembershipRevenueBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "membership-revenue";
    public string Title => Messages.Report_Finance_MembershipRevenue_Title;
    public int Order => 68;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = await RangeAsync(period, cancellationToken);
        return await MembershipRevenueLoader.Relevant(db, period, fromUtc, toUtc).AnyAsync(cancellationToken)
            || await db
                .MembershipRevenueConversions.AsNoTracking()
                .AnyAsync(
                    c => c.BusinessDate >= period.From && c.BusinessDate <= period.To,
                    cancellationToken
                );
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var (fromUtc, toUtc) = await RangeAsync(period, cancellationToken);
        var lines = await MembershipRevenueLoader.LoadAsync(
            db,
            period,
            fromUtc,
            toUtc,
            cancellationToken
        );

        IReadOnlyList<ReportColumn> columns =
        [
            new("plan", Messages.Report_Finance_Col_MembershipPlan, ReportColumnType.Text),
            new("soldCount", Messages.Report_Finance_Col_MembershipSoldCount, ReportColumnType.Number),
            new("soldAmount", Messages.Report_Finance_Col_MembershipSoldAmount, ReportColumnType.Money),
            new("opening", Messages.Report_Finance_Col_MembershipDeferredOpening, ReportColumnType.Money),
            new("recognized", Messages.Report_Finance_Col_MembershipRecognized, ReportColumnType.Money),
            new("converted", Messages.Report_Finance_Col_MembershipConverted, ReportColumnType.Money),
            new("net", Messages.Report_Finance_Col_MembershipNetRevenue, ReportColumnType.Money),
            new("stopped", Messages.Report_Finance_Col_MembershipStopped, ReportColumnType.Money),
            new("closing", Messages.Report_Finance_Col_MembershipDeferredClosing, ReportColumnType.Money),
        ];

        var rows = lines
            .Select(l =>
                (IReadOnlyDictionary<string, object?>)
                    new Dictionary<string, object?>
                    {
                        ["plan"] = l.Plan,
                        ["soldCount"] = l.SoldCount,
                        ["soldAmount"] = l.SoldAmount,
                        ["opening"] = l.Opening,
                        ["recognized"] = l.Recognized,
                        ["converted"] = l.Converted,
                        ["net"] = l.Recognized - l.Converted,
                        ["stopped"] = l.Stopped,
                        ["closing"] = l.Closing,
                    }
            )
            .ToList();

        var totals = new Dictionary<string, object?>
        {
            ["plan"] = Messages.Export_Total,
            ["soldCount"] = lines.Sum(l => l.SoldCount),
            ["soldAmount"] = lines.Sum(l => l.SoldAmount),
            ["opening"] = lines.Sum(l => l.Opening),
            ["recognized"] = lines.Sum(l => l.Recognized),
            ["converted"] = lines.Sum(l => l.Converted),
            ["net"] = lines.Sum(l => l.Recognized - l.Converted),
            ["stopped"] = lines.Sum(l => l.Stopped),
            ["closing"] = lines.Sum(l => l.Closing),
        };

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            totals,
            [Messages.Report_Finance_MembershipRevenue_Note]
        );
    }

    private async Task<(DateTime FromUtc, DateTime ToUtc)> RangeAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        return (
            BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime),
            BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime)
        );
    }
}
