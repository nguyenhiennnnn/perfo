using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class ComboPreorderBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    private const int ChunkSize = 1000;

    public string Key => "combo-preorder";
    public string Title => Messages.Report_Finance_ComboPreorder_Title;
    public int Order => 52;

    private sealed class DayBucket
    {
        public int TicketOrders;
        public int SupplementedOrders;
        public int PreorderUnits;
        public decimal PreorderRevenue;
        public int PreorderPickedUp;
        public int CounterUnits;
        public decimal CounterRevenue;
    }

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var fromUtc = BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime);
        return await db.Orders.AnyAsync(
            o =>
                o.Type == OrderType.Supplement
                && o.OriginalOrderId != null
                && o.Status == OrderStatus.Confirmed
                && o.Lines.Any(l => l.LineType == OrderLineType.Combo)
                && o.CreatedAt >= fromUtc
                && o.CreatedAt < toUtc,
            cancellationToken
        );
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var fromUtc = BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime);
        var days = new Dictionary<DateOnly, DayBucket>();

        DayBucket Day(DateOnly date)
        {
            if (!days.TryGetValue(date, out var bucket))
                days[date] = bucket = new DayBucket();
            return bucket;
        }

        var ticketOrders = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.Type == OrderType.Ticket
                && o.Status == OrderStatus.Confirmed
                && o.CreatedAt >= fromUtc
                && o.CreatedAt < toUtc
            )
            .Select(o => new
            {
                o.CreatedAt,
                Supplemented = o.SupplementOrders.Any(s =>
                    s.Status == OrderStatus.Confirmed
                    && s.Lines.Any(l => l.LineType == OrderLineType.Combo)
                ),
            })
            .ToListAsync(cancellationToken);
        foreach (var order in ticketOrders)
        {
            var bucket = Day(
                BusinessDay.Of(order.CreatedAt, settings.OpeningTime, settings.ClosingTime)
            );
            bucket.TicketOrders++;
            if (order.Supplemented)
                bucket.SupplementedOrders++;
        }

        var comboLines = (
            await RecognizedRevenueLoader.LoadAsync(
                db,
                settings,
                period.From,
                period.To,
                cancellationToken
            )
        )
            .Where(l => l.Kind == RevenueKind.Combo)
            .ToList();

        var supplementIds = new HashSet<int>();
        foreach (var chunk in comboLines.Select(l => l.OrderId).Distinct().Chunk(ChunkSize))
        {
            var ids = await db
                .Orders.AsNoTracking()
                .Where(o =>
                    chunk.Contains(o.OrderId)
                    && o.Type == OrderType.Supplement
                    && o.OriginalOrderId != null
                )
                .Select(o => o.OrderId)
                .ToListAsync(cancellationToken);
            supplementIds.UnionWith(ids);
        }

        var preorderDates = new Dictionary<int, DateOnly>();
        var preorderSold = new Dictionary<int, int>();
        foreach (var line in comboLines)
        {
            if (supplementIds.Contains(line.OrderId))
            {
                var bucket = Day(line.Date);
                bucket.PreorderUnits += line.SoldQuantity;
                bucket.PreorderRevenue += line.Net;
                preorderDates[line.OrderId] = line.Date;
                preorderSold[line.OrderId] =
                    preorderSold.GetValueOrDefault(line.OrderId) + line.SoldQuantity;
            }
            else if (line.Channel == SalesChannel.Counter)
            {
                var bucket = Day(line.Date);
                bucket.CounterUnits += line.SoldQuantity;
                bucket.CounterRevenue += line.Net;
            }
        }

        var fulfilled = new Dictionary<int, int>();
        foreach (var chunk in supplementIds.Chunk(ChunkSize))
        {
            var rows = await db
                .OrderLines.AsNoTracking()
                .Where(l => chunk.Contains(l.OrderId) && l.LineType == OrderLineType.Combo)
                .GroupBy(l => l.OrderId)
                .Select(g => new { OrderId = g.Key, Fulfilled = g.Sum(l => l.QuantityFulfilled) })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
                fulfilled[row.OrderId] = row.Fulfilled;
        }
        foreach (var (orderId, date) in preorderDates)
            Day(date).PreorderPickedUp += Math.Min(
                fulfilled.GetValueOrDefault(orderId),
                preorderSold.GetValueOrDefault(orderId)
            );

        var rows2 = days
            .OrderBy(d => d.Key)
            .Select(d => ToRow(d.Key.ToString(DateTimePatterns.Date), d.Value))
            .ToList();

        var total = new DayBucket
        {
            TicketOrders = days.Values.Sum(d => d.TicketOrders),
            SupplementedOrders = days.Values.Sum(d => d.SupplementedOrders),
            PreorderUnits = days.Values.Sum(d => d.PreorderUnits),
            PreorderRevenue = days.Values.Sum(d => d.PreorderRevenue),
            PreorderPickedUp = days.Values.Sum(d => d.PreorderPickedUp),
            CounterUnits = days.Values.Sum(d => d.CounterUnits),
            CounterRevenue = days.Values.Sum(d => d.CounterRevenue),
        };

        IReadOnlyList<ReportColumn> columns =
        [
            new("date", Messages.Report_Finance_Col_Date, ReportColumnType.Text),
            new("ticketOrders", Messages.Report_Finance_Col_TicketOrders, ReportColumnType.Number),
            new("supplementedOrders", Messages.Report_Finance_Col_SupplementedOrders, ReportColumnType.Number),
            new("supplementRate", Messages.Report_Finance_Col_SupplementRate, ReportColumnType.Percent),
            new("preorderUnits", Messages.Report_Finance_Col_PreorderUnits, ReportColumnType.Number),
            new("preorderRevenue", Messages.Report_Finance_Col_PreorderRevenue, ReportColumnType.Money),
            new("counterUnits", Messages.Report_Finance_Col_CounterComboUnits, ReportColumnType.Number),
            new("counterRevenue", Messages.Report_Finance_Col_CounterComboRevenue, ReportColumnType.Money),
            new("preorderShare", Messages.Report_Finance_Col_PreorderRevenueShare, ReportColumnType.Percent),
            new("preorderPickedUp", Messages.Report_Finance_Col_PreorderPickedUp, ReportColumnType.Number),
            new("preorderPickupRate", Messages.Report_Finance_Col_PreorderPickupRate, ReportColumnType.Percent),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows2,
            ToRow(Messages.Export_Total, total),
            [Messages.Report_Finance_ComboPreorder_Note]
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(string label, DayBucket b) =>
        new Dictionary<string, object?>
        {
            ["date"] = label,
            ["ticketOrders"] = b.TicketOrders,
            ["supplementedOrders"] = b.SupplementedOrders,
            ["supplementRate"] = ReportMath.Percent(b.SupplementedOrders, b.TicketOrders),
            ["preorderUnits"] = b.PreorderUnits,
            ["preorderRevenue"] = b.PreorderRevenue,
            ["counterUnits"] = b.CounterUnits,
            ["counterRevenue"] = b.CounterRevenue,
            ["preorderShare"] = ReportMath.Percent(
                b.PreorderRevenue,
                b.PreorderRevenue + b.CounterRevenue
            ),
            ["preorderPickedUp"] = b.PreorderPickedUp,
            ["preorderPickupRate"] = ReportMath.Percent(b.PreorderPickedUp, b.PreorderUnits),
        };
}
