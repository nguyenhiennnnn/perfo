using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class ExchangeHotspotBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    private const int TopCount = 20;

    public string Key => "exchange-hotspots";
    public string Title => Messages.Report_Finance_ExchangeHotspots_Title;
    public int Order => 36;

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
                MovieId = e.OldShowtime.MovieId,
                Title = e.OldShowtime.Movie.Title,
                Start = e.OldShowtime.StartTime,
            })
            .ToListAsync(cancellationToken);

        var totalExchanges = lines.Select(l => (l.OrderId, l.ExchangeNumber)).Distinct().Count();

        var groups = lines
            .GroupBy(l => new
            {
                l.MovieId,
                l.Title,
                Hour = BusinessTime.ToLocal(l.Start).Hour,
            })
            .Select(g => new
            {
                g.Key.Title,
                g.Key.Hour,
                Exchanges = g.Select(l => (l.OrderId, l.ExchangeNumber)).Distinct().Count(),
                Tickets = g.Count(),
            })
            .OrderByDescending(g => g.Exchanges)
            .ThenByDescending(g => g.Tickets)
            .ThenBy(g => g.Title)
            .ThenBy(g => g.Hour)
            .Take(TopCount)
            .ToList();

        var rows = groups
            .Select(g =>
                (IReadOnlyDictionary<string, object?>)
                    new Dictionary<string, object?>
                    {
                        ["movie"] = g.Title,
                        ["slot"] = $"{g.Hour:00}:00 - {(g.Hour + 1) % 24:00}:00",
                        ["exchangeCount"] = g.Exchanges,
                        ["exchangedTickets"] = g.Tickets,
                        ["share"] = ReportMath.Percent(g.Exchanges, totalExchanges),
                    }
            )
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("movie", Messages.Report_Finance_Col_Movie, ReportColumnType.Text),
            new("slot", Messages.Report_Finance_Col_TimeSlot, ReportColumnType.Text),
            new("exchangeCount", Messages.Report_Finance_Col_ExchangeCount, ReportColumnType.Number),
            new("exchangedTickets", Messages.Report_Finance_Col_ExchangedTickets, ReportColumnType.Number),
            new("share", Messages.Report_Finance_Col_ExchangeShare, ReportColumnType.Percent),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            null,
            [Messages.Report_Finance_ExchangeHotspots_Note]
        );
    }
}
