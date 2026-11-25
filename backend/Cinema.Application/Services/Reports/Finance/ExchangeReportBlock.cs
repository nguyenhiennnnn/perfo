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

public class ExchangeReportBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "exchanges";
    public string Title => Messages.Report_Finance_Exchanges_Title;
    public int Order => 35;

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var window = await ExchangeReportSupport.WindowAsync(cache, period, cancellationToken);
        return await ExchangeReportSupport
            .InWindow(db, window.FromUtc, window.ToUtc)
            .AnyAsync(cancellationToken);
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var window = await ExchangeReportSupport.WindowAsync(cache, period, cancellationToken);

        var lines = await ExchangeReportSupport
            .InWindow(db, window.FromUtc, window.ToUtc)
            .Select(e => new
            {
                e.OrderId,
                e.ExchangeNumber,
                e.ExchangedAt,
                e.PriceDifference,
            })
            .ToListAsync(cancellationToken);

        var events = lines
            .GroupBy(l => new { l.OrderId, l.ExchangeNumber })
            .Select(g => new
            {
                Date = BusinessDay.Of(g.Min(l => l.ExchangedAt), window.Opening, window.Closing),
                Tickets = g.Count(),
                Difference = g.Sum(l => l.PriceDifference),
            })
            .ToList();

        var perDay = events
            .GroupBy(e => e.Date)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Exchanges = g.Count(),
                    Tickets = g.Sum(e => e.Tickets),
                    Surcharge = g.Sum(e => Math.Max(e.Difference, 0m)),
                }
            );

        var credits = (
            await db
                .GiftCards.AsNoTracking()
                .Where(g =>
                    g.Type == GiftCardType.ExchangeCredit
                    && g.CreatedAt >= window.FromUtc
                    && g.CreatedAt < window.ToUtc
                )
                .Select(g => new { g.CreatedAt, g.FaceValue })
                .ToListAsync(cancellationToken)
        )
            .GroupBy(g => BusinessDay.Of(g.CreatedAt, window.Opening, window.Closing))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.FaceValue));

        var cancelled = await db
            .ShowtimeDailyStats.AsNoTracking()
            .Where(s =>
                s.BusinessDate >= period.From
                && s.BusinessDate <= period.To
                && s.Kind == ShowtimeKind.Regular
            )
            .GroupBy(s => s.BusinessDate)
            .Select(g => new { Date = g.Key, Cancelled = g.Sum(s => s.CancelledOrders) })
            .ToDictionaryAsync(x => x.Date, x => x.Cancelled, cancellationToken);

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        int exchangesTotal = 0,
            ticketsTotal = 0,
            cancelledTotal = 0;
        decimal surchargeTotal = 0m,
            creditTotal = 0m;

        foreach (var date in perDay.Keys.Union(credits.Keys).Union(cancelled.Keys).Order())
        {
            var day = perDay.GetValueOrDefault(date);
            var exchanges = day?.Exchanges ?? 0;
            var tickets = day?.Tickets ?? 0;
            var surcharge = day?.Surcharge ?? 0m;
            var credit = credits.GetValueOrDefault(date);
            var cancelledOrders = cancelled.GetValueOrDefault(date);

            exchangesTotal += exchanges;
            ticketsTotal += tickets;
            surchargeTotal += surcharge;
            creditTotal += credit;
            cancelledTotal += cancelledOrders;

            rows.Add(
                ToRow(
                    date.ToString(DateTimePatterns.Date),
                    exchanges,
                    tickets,
                    cancelledOrders,
                    surcharge,
                    credit
                )
            );
        }

        IReadOnlyList<ReportColumn> columns =
        [
            new("date", Messages.Report_Finance_Col_Date, ReportColumnType.Text),
            new("exchangeCount", Messages.Report_Finance_Col_ExchangeCount, ReportColumnType.Number),
            new("exchangedTickets", Messages.Report_Finance_Col_ExchangedTickets, ReportColumnType.Number),
            new("cancelledOrders", Messages.Report_Finance_Col_CancelledOrders, ReportColumnType.Number),
            new("exchangeRate", Messages.Report_Finance_Col_ExchangeVsCancelRate, ReportColumnType.Percent),
            new("surcharge", Messages.Report_Finance_Col_ExchangeSurcharge, ReportColumnType.Money),
            new("creditIssued", Messages.Report_Finance_Col_ExchangeCreditIssued, ReportColumnType.Money),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(
                Messages.Export_Total,
                exchangesTotal,
                ticketsTotal,
                cancelledTotal,
                surchargeTotal,
                creditTotal
            )
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        string date,
        int exchanges,
        int tickets,
        int cancelled,
        decimal surcharge,
        decimal credit
    ) =>
        new Dictionary<string, object?>
        {
            ["date"] = date,
            ["exchangeCount"] = exchanges,
            ["exchangedTickets"] = tickets,
            ["cancelledOrders"] = cancelled,
            ["exchangeRate"] = ReportMath.Percent(exchanges, exchanges + cancelled),
            ["surcharge"] = surcharge,
            ["creditIssued"] = credit,
        };
}
