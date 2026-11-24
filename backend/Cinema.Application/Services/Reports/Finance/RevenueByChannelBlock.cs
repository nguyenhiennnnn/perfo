using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class RevenueByChannelBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "revenue-by-channel";
    public string Title => Messages.Report_Finance_RevenueByChannel_Title;
    public int Order => 10;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        await db.PaymentDailyStats.AnyAsync(
            p => p.BusinessDate >= period.From && p.BusinessDate <= period.To,
            cancellationToken
        )
        || await db.ShowtimeDailyStats.AnyAsync(
            s => s.BusinessDate >= period.From && s.BusinessDate <= period.To,
            cancellationToken
        );

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var lines = await RecognizedRevenueLoader.LoadAsync(
            db,
            settings,
            period.From,
            period.To,
            cancellationToken
        );

        var buckets = await MethodRevenueAllocator.AllocateAsync(db, lines, cancellationToken);
        RevenueBucket BucketOf(SalesChannel channel, PaymentMethod? method)
        {
            if (!buckets.TryGetValue((channel, method), out var bucket))
                buckets[(channel, method)] = bucket = new RevenueBucket();
            return bucket;
        }

        var payments = await db
            .PaymentDailyStats.AsNoTracking()
            .Where(p => p.BusinessDate >= period.From && p.BusinessDate <= period.To)
            .GroupBy(p => new { p.Channel, p.Method })
            .Select(g => new
            {
                g.Key.Channel,
                g.Key.Method,
                Collected = g.Sum(p => p.CollectedAmount),
                Refunded = g.Sum(p => p.RefundedAmount),
            })
            .ToListAsync(cancellationToken);
        foreach (var p in payments)
        {
            var bucket = BucketOf(p.Channel, p.Method);
            bucket.Collected += p.Collected;
            bucket.Refunded += p.Refunded;
        }

        var rowsOut = new List<IReadOnlyDictionary<string, object?>>();
        var total = new RevenueBucket();
        foreach (
            var entry in buckets
                .OrderBy(e => e.Key.Channel)
                .ThenBy(e => e.Key.Method is null)
                .ThenBy(e => e.Key.Method)
        )
        {
            var b = entry.Value;
            total.Ticket += b.Ticket;
            total.Combo += b.Combo;
            total.GiftCard += b.GiftCard;
            total.Collected += b.Collected;
            total.Refunded += b.Refunded;
            rowsOut.Add(
                ToRow(
                    ExportLabels.Channel(entry.Key.Channel),
                    entry.Key.Method is { } m
                        ? ExportLabels.Method(m)
                        : Messages.Report_Finance_Method_Unknown,
                    b
                )
            );
        }

        IReadOnlyList<ReportColumn> columns =
        [
            new("channel", Messages.Export_ColChannel, ReportColumnType.Text),
            new("method", Messages.Export_ColPaymentMethod, ReportColumnType.Text),
            new("ticketRevenue", Messages.Export_ColTicketRevenue, ReportColumnType.Money),
            new("comboRevenue", Messages.Export_ColComboRevenue, ReportColumnType.Money),
            new("totalRevenue", Messages.Export_ColTotalRevenue, ReportColumnType.Money),
            new("giftCardSold", Messages.Report_Finance_Col_GiftCardSold, ReportColumnType.Money),
            new("collected", Messages.Report_Finance_Col_Collected, ReportColumnType.Money),
            new("refunded", Messages.Report_Finance_Col_Refunded, ReportColumnType.Money),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rowsOut,
            ToRow(Messages.Export_Total, string.Empty, total)
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        string channel,
        string method,
        RevenueBucket b
    ) =>
        new Dictionary<string, object?>
        {
            ["channel"] = channel,
            ["method"] = method,
            ["ticketRevenue"] = b.Ticket,
            ["comboRevenue"] = b.Combo,
            ["totalRevenue"] = b.Ticket + b.Combo,
            ["giftCardSold"] = b.GiftCard,
            ["collected"] = b.Collected,
            ["refunded"] = b.Refunded,
        };
}
