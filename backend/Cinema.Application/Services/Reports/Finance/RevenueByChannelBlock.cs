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
    private const int ChunkSize = 1000;

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

        var orderIds = lines.Select(l => l.OrderId).Distinct().ToList();
        var transactions = new List<(int OrderId, PaymentMethod Method, PaymentTransactionType Type, decimal Amount)>();
        foreach (var chunk in orderIds.Chunk(ChunkSize))
        {
            var rows = await db
                .PaymentTransactions.AsNoTracking()
                .Where(t => chunk.Contains(t.OrderId) && t.Status == PaymentStatus.Success)
                .Select(t => new
                {
                    t.OrderId,
                    t.Method,
                    t.Type,
                    t.Amount,
                })
                .ToListAsync(cancellationToken);
            transactions.AddRange(rows.Select(r => (r.OrderId, r.Method, r.Type, r.Amount)));
        }

        var weightsByOrder = transactions
            .GroupBy(t => t.OrderId)
            .ToDictionary(g => g.Key, g => MethodWeights(g.ToList()));

        var buckets = new Dictionary<(SalesChannel Channel, PaymentMethod? Method), Bucket>();
        Bucket BucketOf(SalesChannel channel, PaymentMethod? method)
        {
            if (!buckets.TryGetValue((channel, method), out var bucket))
                buckets[(channel, method)] = bucket = new Bucket();
            return bucket;
        }

        foreach (var group in lines.GroupBy(l => (l.OrderId, l.Channel, l.Kind)))
        {
            var amount = group.Sum(l => l.Net);
            if (amount == 0m)
                continue;

            var weights = weightsByOrder.GetValueOrDefault(group.Key.OrderId) ?? [];
            if (weights.Count == 0)
            {
                Add(BucketOf(group.Key.Channel, null), group.Key.Kind, amount);
                continue;
            }

            var shares = Spread(amount, weights.Select(w => w.Weight).ToList());
            for (var i = 0; i < weights.Count; i++)
                Add(BucketOf(group.Key.Channel, weights[i].Method), group.Key.Kind, shares[i]);
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
        var total = new Bucket();
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
        Bucket b
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

    private static void Add(Bucket bucket, RevenueKind kind, decimal amount)
    {
        switch (kind)
        {
            case RevenueKind.Ticket:
                bucket.Ticket += amount;
                break;
            case RevenueKind.Combo:
                bucket.Combo += amount;
                break;
            default:
                bucket.GiftCard += amount;
                break;
        }
    }

    private static List<(PaymentMethod Method, decimal Weight)> MethodWeights(
        List<(int OrderId, PaymentMethod Method, PaymentTransactionType Type, decimal Amount)> rows
    )
    {
        List<(PaymentMethod Method, decimal Weight)> Weigh(Func<IGrouping<PaymentMethod, (int OrderId, PaymentMethod Method, PaymentTransactionType Type, decimal Amount)>, decimal> weight) =>
            rows.GroupBy(r => r.Method)
                .Select(g => (g.Key, weight(g)))
                .Where(x => x.Item2 > 0m)
                .OrderBy(x => x.Item2)
                .ThenBy(x => x.Key)
                .ToList();

        var net = Weigh(g =>
            g.Where(r => r.Type == PaymentTransactionType.Charge).Sum(r => r.Amount)
            - g.Where(r => r.Type == PaymentTransactionType.Refund).Sum(r => r.Amount)
        );
        return net.Count > 0
            ? net
            : Weigh(g => g.Where(r => r.Type == PaymentTransactionType.Charge).Sum(r => r.Amount));
    }

    private static decimal[] Spread(decimal amount, IReadOnlyList<decimal> weights)
    {
        var shares = new decimal[weights.Count];
        var total = weights.Sum();
        if (total <= 0m)
            return shares;

        var largest = 0;
        for (var i = 0; i < weights.Count; i++)
        {
            shares[i] = Math.Floor(amount * weights[i] / total);
            if (weights[i] > weights[largest])
                largest = i;
        }

        shares[largest] += amount - shares.Sum();
        return shares;
    }

    private sealed class Bucket
    {
        public decimal Ticket { get; set; }
        public decimal Combo { get; set; }
        public decimal GiftCard { get; set; }
        public decimal Collected { get; set; }
        public decimal Refunded { get; set; }
    }
}
