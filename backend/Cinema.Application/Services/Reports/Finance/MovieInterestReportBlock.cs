using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class MovieInterestReportBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    private const int TopCount = 50;

    public string Key => "movie-interest";
    public string Title => Messages.Report_Finance_MovieInterest_Title;
    public int Order => 63;

    public Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        db.MovieInterests.AnyAsync(cancellationToken);

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var fromUtc = BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime);
        var toUtc = BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime);

        var groups = await db
            .MovieInterests.AsNoTracking()
            .GroupBy(i => new
            {
                i.MovieId,
                i.Movie.Title,
                i.Movie.ReleaseDate,
            })
            .Select(g => new
            {
                g.Key.Title,
                g.Key.ReleaseDate,
                Total = g.Count(),
                New = g.Count(i => i.CreatedAt >= fromUtc && i.CreatedAt < toUtc),
                Notified = g.Count(i => i.NotifiedAt != null),
            })
            .ToListAsync(cancellationToken);

        var shown = groups
            .OrderByDescending(g => g.Total)
            .ThenBy(g => g.ReleaseDate)
            .ThenBy(g => g.Title)
            .Take(TopCount)
            .Select(g =>
                (IReadOnlyDictionary<string, object?>)
                    new Dictionary<string, object?>
                    {
                        ["movie"] = g.Title,
                        ["releaseDate"] = g.ReleaseDate?.ToString(DateTimePatterns.Date),
                        ["total"] = g.Total,
                        ["new"] = g.New,
                        ["notified"] = g.Notified,
                    }
            )
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("movie", Messages.Report_Finance_Col_Movie, ReportColumnType.Text),
            new("releaseDate", Messages.Report_Finance_Col_ReleaseDate, ReportColumnType.Text),
            new("total", Messages.Report_Finance_Col_InterestTotal, ReportColumnType.Number),
            new("new", Messages.Report_Finance_Col_InterestNew, ReportColumnType.Number),
            new("notified", Messages.Report_Finance_Col_InterestNotified, ReportColumnType.Number),
        ];

        var totals = new Dictionary<string, object?>
        {
            ["movie"] = Messages.Export_Total,
            ["releaseDate"] = null,
            ["total"] = groups.Sum(g => g.Total),
            ["new"] = groups.Sum(g => g.New),
            ["notified"] = groups.Sum(g => g.Notified),
        };

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            shown,
            totals,
            [Messages.Report_Finance_MovieInterest_Note]
        );
    }
}
