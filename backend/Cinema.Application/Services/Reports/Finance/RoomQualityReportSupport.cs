using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Utils;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

internal sealed record RoomQualitySurveyRow(
    int RoomId,
    string RoomName,
    DateOnly BusinessDate,
    int Room,
    int Sound,
    int Picture,
    int Cleanliness,
    int CounterService
)
{
    public decimal RoomScore => (Room + Sound + Picture + Cleanliness) / 4m;
}

internal static class RoomQualityReportSupport
{
    public static async Task<IReadOnlyList<RoomQualitySurveyRow>> LoadAsync(
        IAppDbContext db,
        IAppCache cache,
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var window = await ExchangeReportSupport.WindowAsync(cache, period, cancellationToken);
        var rows = await db
            .ExperienceSurveys.AsNoTracking()
            .Where(s => s.CreatedAt >= window.FromUtc && s.CreatedAt < window.ToUtc)
            .Select(s => new
            {
                s.Showtime.RoomId,
                s.Showtime.Room.RoomName,
                s.CreatedAt,
                s.RoomRating,
                s.SoundRating,
                s.PictureRating,
                s.CleanlinessRating,
                s.CounterServiceRating,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new RoomQualitySurveyRow(
                r.RoomId,
                r.RoomName,
                BusinessDay.Of(r.CreatedAt, window.Opening, window.Closing),
                r.RoomRating,
                r.SoundRating,
                r.PictureRating,
                r.CleanlinessRating,
                r.CounterServiceRating
            ))
            .ToList();
    }

    public static async Task<bool> AnyAsync(
        IAppDbContext db,
        IAppCache cache,
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var window = await ExchangeReportSupport.WindowAsync(cache, period, cancellationToken);
        return await db
            .ExperienceSurveys.AsNoTracking()
            .AnyAsync(
                s => s.CreatedAt >= window.FromUtc && s.CreatedAt < window.ToUtc,
                cancellationToken
            );
    }

    public static decimal Average(IEnumerable<RoomQualitySurveyRow> rows, Func<RoomQualitySurveyRow, decimal> selector)
    {
        var list = rows as IReadOnlyCollection<RoomQualitySurveyRow> ?? rows.ToList();
        return list.Count == 0
            ? 0m
            : Math.Round(list.Average(selector), 2, MidpointRounding.AwayFromZero);
    }
}
