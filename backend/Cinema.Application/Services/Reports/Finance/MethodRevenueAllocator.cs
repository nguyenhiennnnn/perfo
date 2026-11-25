using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

internal sealed class RevenueBucket
{
    public decimal Ticket { get; set; }
    public decimal Combo { get; set; }
    public decimal GiftCard { get; set; }
    public decimal Collected { get; set; }
    public decimal Refunded { get; set; }
}

internal static class ReportMath
{
    public static decimal? Percent(decimal part, decimal whole) =>
        whole > 0m ? Math.Round(part / whole * 100m, 2, MidpointRounding.AwayFromZero) : null;
}

internal static class MethodRevenueAllocator
{
    private const int ChunkSize = 1000;

    private sealed record Tx(int OrderId, PaymentMethod Method, PaymentTransactionType Type, decimal Amount);

    public static async Task<Dictionary<(SalesChannel Channel, PaymentMethod? Method), RevenueBucket>> AllocateAsync(
        IAppDbContext db,
        IReadOnlyList<RecognizedLine> lines,
        CancellationToken cancellationToken
    )
    {
        var orderIds = lines.Select(l => l.OrderId).Distinct().ToList();
        var transactions = new List<Tx>();
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
            transactions.AddRange(rows.Select(r => new Tx(r.OrderId, r.Method, r.Type, r.Amount)));
        }

        var weightsByOrder = transactions
            .GroupBy(t => t.OrderId)
            .ToDictionary(g => g.Key, g => MethodWeights(g.ToList()));

        var buckets = new Dictionary<(SalesChannel Channel, PaymentMethod? Method), RevenueBucket>();
        RevenueBucket BucketOf(SalesChannel channel, PaymentMethod? method)
        {
            if (!buckets.TryGetValue((channel, method), out var bucket))
                buckets[(channel, method)] = bucket = new RevenueBucket();
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

        return buckets;
    }

    private static void Add(RevenueBucket bucket, RevenueKind kind, decimal amount)
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

    private static List<(PaymentMethod Method, decimal Weight)> MethodWeights(List<Tx> rows)
    {
        List<(PaymentMethod Method, decimal Weight)> Weigh(
            Func<IGrouping<PaymentMethod, Tx>, decimal> weight
        ) =>
            rows.GroupBy(r => r.Method)
                .Select(g => (Method: g.Key, Weight: weight(g)))
                .Where(x => x.Weight > 0m)
                .OrderBy(x => x.Weight)
                .ThenBy(x => x.Method)
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
}
