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

public class WaitlistReportBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    private const int ChunkSize = 1000;

    public string Key => "waitlist";
    public string Title => Messages.Report_Finance_Waitlist_Title;
    public int Order => 62;

    private sealed record EntryRow(
        int ShowtimeId,
        int UserId,
        int SeatCount,
        WaitlistStatus Status,
        DateTime? NotifiedAt
    );

    private sealed record ShowtimeRow(int ShowtimeId, string Movie, DateTime StartTime);

    private sealed class Bucket
    {
        public int Registrations;
        public int Seats;
        public int Notified;
        public int Purchased;
        public int Withdrawn;
    }

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = await WindowAsync(period, cancellationToken);
        return await db.WaitlistEntries.AnyAsync(
            e => e.Showtime.StartTime >= fromUtc && e.Showtime.StartTime < toUtc,
            cancellationToken
        );
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var (fromUtc, toUtc) = await WindowAsync(period, cancellationToken);

        var entries = await db
            .WaitlistEntries.AsNoTracking()
            .Where(e => e.Showtime.StartTime >= fromUtc && e.Showtime.StartTime < toUtc)
            .Select(e => new EntryRow(
                e.ShowtimeId,
                e.UserId,
                e.SeatCount,
                e.Status,
                e.NotifiedAt
            ))
            .ToListAsync(cancellationToken);

        var showtimeIds = entries.Select(e => e.ShowtimeId).Distinct().ToList();
        var showtimes = new List<ShowtimeRow>();
        var purchases = new List<(int UserId, int ShowtimeId, DateTime CreatedAt)>();
        foreach (var chunk in showtimeIds.Chunk(ChunkSize))
        {
            showtimes.AddRange(
                await db
                    .Showtimes.AsNoTracking()
                    .Where(s => chunk.Contains(s.ShowtimeId))
                    .Select(s => new ShowtimeRow(s.ShowtimeId, s.Movie.Title, s.StartTime))
                    .ToListAsync(cancellationToken)
            );

            var rows = await db
                .OrderLines.AsNoTracking()
                .Where(l =>
                    l.LineType == OrderLineType.Ticket
                    && l.ShowtimeId != null
                    && chunk.Contains(l.ShowtimeId.Value)
                    && l.Order.Type == OrderType.Ticket
                    && l.Order.Status == OrderStatus.Confirmed
                    && l.Order.UserId != null
                )
                .Select(l => new
                {
                    UserId = l.Order.UserId!.Value,
                    ShowtimeId = l.ShowtimeId!.Value,
                    l.Order.CreatedAt,
                })
                .Distinct()
                .ToListAsync(cancellationToken);
            purchases.AddRange(rows.Select(r => (r.UserId, r.ShowtimeId, r.CreatedAt)));
        }

        var purchaseLookup = purchases
            .GroupBy(p => (p.UserId, p.ShowtimeId))
            .ToDictionary(g => g.Key, g => g.Min(p => p.CreatedAt));

        var buckets = new Dictionary<int, Bucket>();
        foreach (var entry in entries)
        {
            if (!buckets.TryGetValue(entry.ShowtimeId, out var bucket))
                buckets[entry.ShowtimeId] = bucket = new Bucket();
            bucket.Registrations++;
            bucket.Seats += entry.SeatCount;
            if (entry.Status == WaitlistStatus.Withdrawn)
                bucket.Withdrawn++;
            if (entry.NotifiedAt is null)
                continue;
            bucket.Notified++;
            if (
                purchaseLookup.TryGetValue((entry.UserId, entry.ShowtimeId), out var firstPurchase)
                && firstPurchase >= entry.NotifiedAt
            )
                bucket.Purchased++;
        }

        var rowsOut = showtimes
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.ShowtimeId)
            .Select(s => ToRow(
                s.Movie,
                BusinessTime.ToLocal(s.StartTime).ToString(DateTimePatterns.DateHourMinute),
                buckets[s.ShowtimeId]
            ))
            .ToList();

        var total = new Bucket
        {
            Registrations = buckets.Values.Sum(b => b.Registrations),
            Seats = buckets.Values.Sum(b => b.Seats),
            Notified = buckets.Values.Sum(b => b.Notified),
            Purchased = buckets.Values.Sum(b => b.Purchased),
            Withdrawn = buckets.Values.Sum(b => b.Withdrawn),
        };

        IReadOnlyList<ReportColumn> columns =
        [
            new("movie", Messages.Report_Finance_Col_Movie, ReportColumnType.Text),
            new("showtime", Messages.Report_Finance_Col_Showtime, ReportColumnType.Text),
            new("registrations", Messages.Report_Finance_Col_WaitlistRegistrations, ReportColumnType.Number),
            new("seats", Messages.Report_Finance_Col_WaitlistSeats, ReportColumnType.Number),
            new("notified", Messages.Report_Finance_Col_WaitlistNotified, ReportColumnType.Number),
            new("purchased", Messages.Report_Finance_Col_WaitlistPurchased, ReportColumnType.Number),
            new("conversion", Messages.Report_Finance_Col_WaitlistConversion, ReportColumnType.Percent),
            new("withdrawn", Messages.Report_Finance_Col_WaitlistWithdrawn, ReportColumnType.Number),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rowsOut,
            ToRow(Messages.Export_Total, null, total),
            [Messages.Report_Finance_Waitlist_Note]
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

    private static IReadOnlyDictionary<string, object?> ToRow(
        string movie,
        string? showtime,
        Bucket b
    ) =>
        new Dictionary<string, object?>
        {
            ["movie"] = movie,
            ["showtime"] = showtime,
            ["registrations"] = b.Registrations,
            ["seats"] = b.Seats,
            ["notified"] = b.Notified,
            ["purchased"] = b.Purchased,
            ["conversion"] = ReportMath.Percent(b.Purchased, b.Notified),
            ["withdrawn"] = b.Withdrawn,
        };
}
