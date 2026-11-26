using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class BundleReportBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "bundles";
    public string Title => Messages.Report_Finance_Bundle_Title;
    public int Order => 53;

    private sealed record LineRow(decimal Retail, decimal Split, decimal Paid, decimal Refunded);

    private sealed record SaleRow(
        int BundleId,
        int? UserId,
        int? ShowtimeId,
        List<LineRow> Lines
    );

    private sealed class Bucket
    {
        public int Sold;
        public decimal Retail;
        public decimal Savings;
        public decimal Net;
        public int Viewers;
        public int Buyers;
    }

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = await WindowAsync(period, cancellationToken);
        return await db.OrderPriceAdjustments.AnyAsync(
            a =>
                a.Type == PriceAdjustmentType.PackagePrice
                && a.Order.Status == OrderStatus.Confirmed
                && a.Allocations.Any(x =>
                    x.OrderLine.Showtime!.StartTime >= fromUtc
                    && x.OrderLine.Showtime.StartTime < toUtc
                ),
            cancellationToken
        );
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var (fromUtc, toUtc) = await WindowAsync(period, cancellationToken);

        var sales = await db
            .OrderPriceAdjustments.AsNoTracking()
            .Where(a =>
                a.Type == PriceAdjustmentType.PackagePrice
                && a.ReferenceType == nameof(Bundle)
                && a.ReferenceId != null
                && a.Order.Status == OrderStatus.Confirmed
                && a.Allocations.Any(x =>
                    x.OrderLine.Showtime!.StartTime >= fromUtc
                    && x.OrderLine.Showtime.StartTime < toUtc
                )
            )
            .Select(a => new SaleRow(
                a.ReferenceId!.Value,
                a.Order.UserId,
                a.Allocations.Select(x => x.OrderLine.ShowtimeId).FirstOrDefault(),
                a.Allocations.Select(x => new LineRow(
                        x.OrderLine.LockedUnitPrice * x.OrderLine.Quantity,
                        x.Amount,
                        x.OrderLine.PaidAmount,
                        x.OrderLine.RefundedAmount
                    ))
                    .ToList()
            ))
            .ToListAsync(cancellationToken);

        var views = await db
            .BundleViews.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(v => v.Showtime.StartTime >= fromUtc && v.Showtime.StartTime < toUtc)
            .Select(v => new
            {
                v.BundleId,
                v.ShowtimeId,
                v.UserId,
            })
            .ToListAsync(cancellationToken);

        var buyerKeys = sales
            .Where(s => s.UserId != null && s.ShowtimeId != null)
            .Select(s => (s.BundleId, s.UserId!.Value, s.ShowtimeId!.Value))
            .ToHashSet();

        var buckets = new Dictionary<int, Bucket>();
        Bucket For(int bundleId) =>
            buckets.TryGetValue(bundleId, out var b) ? b : buckets[bundleId] = new Bucket();

        foreach (var sale in sales)
        {
            var bucket = For(sale.BundleId);
            bucket.Sold++;
            bucket.Retail += sale.Lines.Sum(l => l.Retail);
            bucket.Savings += sale.Lines.Sum(l => l.Split);
            bucket.Net += sale.Lines.Sum(l => l.Paid - l.Refunded);
        }

        foreach (var view in views)
        {
            var bucket = For(view.BundleId);
            bucket.Viewers++;
            if (buyerKeys.Contains((view.BundleId, view.UserId, view.ShowtimeId)))
                bucket.Buyers++;
        }

        var bundleIds = buckets.Keys.ToList();
        var names = await db
            .Bundles.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => bundleIds.Contains(b.BundleId))
            .ToDictionaryAsync(b => b.BundleId, b => b.Name, cancellationToken);

        var rows = buckets
            .OrderByDescending(b => b.Value.Net)
            .ThenBy(b => b.Key)
            .Select(b => ToRow(names.GetValueOrDefault(b.Key) ?? $"#{b.Key}", b.Value))
            .ToList();

        var total = new Bucket
        {
            Sold = buckets.Values.Sum(b => b.Sold),
            Retail = buckets.Values.Sum(b => b.Retail),
            Savings = buckets.Values.Sum(b => b.Savings),
            Net = buckets.Values.Sum(b => b.Net),
            Viewers = buckets.Values.Sum(b => b.Viewers),
            Buyers = buckets.Values.Sum(b => b.Buyers),
        };

        IReadOnlyList<ReportColumn> columns =
        [
            new("bundle", Messages.Report_Finance_Col_Bundle, ReportColumnType.Text),
            new("sold", Messages.Report_Finance_Col_BundleSold, ReportColumnType.Number),
            new("retail", Messages.Report_Finance_Col_BundleRetail, ReportColumnType.Money),
            new("revenue", Messages.Report_Finance_Col_BundleRevenue, ReportColumnType.Money),
            new("savings", Messages.Report_Finance_Col_BundleSavings, ReportColumnType.Money),
            new("netRevenue", Messages.Report_Finance_Col_BundleNetRevenue, ReportColumnType.Money),
            new("viewers", Messages.Report_Finance_Col_BundleViewers, ReportColumnType.Number),
            new("buyers", Messages.Report_Finance_Col_BundleBuyers, ReportColumnType.Number),
            new("conversion", Messages.Report_Finance_Col_BundleConversion, ReportColumnType.Percent),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(Messages.Export_Total, total),
            [Messages.Report_Finance_Bundle_Note]
        );
    }

    private async Task<(DateTime FromUtc, DateTime ToUtc)> WindowAsync(
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

    private static IReadOnlyDictionary<string, object?> ToRow(string name, Bucket b) =>
        new Dictionary<string, object?>
        {
            ["bundle"] = name,
            ["sold"] = b.Sold,
            ["retail"] = b.Retail,
            ["revenue"] = b.Retail - b.Savings,
            ["savings"] = b.Savings,
            ["netRevenue"] = b.Net,
            ["viewers"] = b.Viewers,
            ["buyers"] = b.Buyers,
            ["conversion"] = ReportMath.Percent(b.Buyers, b.Viewers),
        };
}
