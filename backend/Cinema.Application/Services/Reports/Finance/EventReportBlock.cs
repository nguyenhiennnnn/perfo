using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class EventReportBlock(IAppDbContext db) : IReportBlock
{
    public string Key => "event";
    public string Title => Messages.Report_Finance_Event_Title;
    public int Order => 66;

    private sealed record Row(
        string Movie,
        string Showtime,
        string Room,
        int Tickets,
        decimal Revenue,
        int Sellable,
        int GiftsGiven
    );

    public Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken) =>
        EventStats(period).AnyAsync(cancellationToken);

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var stats = await EventStats(period)
            .OrderBy(d => d.Showtime.StartTime)
            .ThenBy(d => d.ShowtimeId)
            .Select(d => new
            {
                d.ShowtimeId,
                d.Showtime.StartTime,
                MovieTitle = d.Showtime.Movie.Title,
                RoomName = d.Showtime.Room.RoomName,
                d.TicketsSold,
                d.SellableSeats,
                Revenue = d.GrossTicketRevenue - d.DiscountAmount - d.RefundAmount,
            })
            .ToListAsync(cancellationToken);

        var showtimeIds = stats.Select(s => s.ShowtimeId).ToList();
        var gifts = await db
            .ShowtimeEventGiftReservations.AsNoTracking()
            .Where(r => showtimeIds.Contains(r.ShowtimeId))
            .GroupBy(r => r.ShowtimeId)
            .Select(g => new { ShowtimeId = g.Key, Given = g.Sum(r => r.QuantityGiven) })
            .ToDictionaryAsync(x => x.ShowtimeId, x => x.Given, cancellationToken);

        var rows = stats
            .Select(s => new Row(
                s.MovieTitle,
                BusinessTime.ToLocal(s.StartTime).ToString(DateTimePatterns.DateHourMinute),
                s.RoomName,
                s.TicketsSold,
                s.Revenue,
                s.SellableSeats,
                gifts.GetValueOrDefault(s.ShowtimeId)
            ))
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("movie", Messages.Report_Finance_Col_Movie, ReportColumnType.Text),
            new("showtime", Messages.Report_Finance_Col_Showtime, ReportColumnType.Text),
            new("room", Messages.Report_Finance_Col_EventRoom, ReportColumnType.Text),
            new("tickets", Messages.Report_Finance_Col_EventTickets, ReportColumnType.Number),
            new("revenue", Messages.Report_Finance_Col_EventRevenue, ReportColumnType.Money),
            new("occupancy", Messages.Report_Finance_Col_EventOccupancy, ReportColumnType.Percent),
            new("giftsGiven", Messages.Report_Finance_Col_EventGiftsGiven, ReportColumnType.Number),
        ];

        var total = new Row(
            string.Empty,
            string.Empty,
            string.Empty,
            rows.Sum(r => r.Tickets),
            rows.Sum(r => r.Revenue),
            rows.Sum(r => r.Sellable),
            rows.Sum(r => r.GiftsGiven)
        );

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows.Select(r => ToRow(r.Movie, r.Showtime, r.Room, r)).ToList(),
            ToRow(Messages.Export_Total, null, null, total),
            [Messages.Report_Finance_Event_Note]
        );
    }

    private IQueryable<ShowtimeDailyStat> EventStats(ReportPeriod period) =>
        db.ShowtimeDailyStats.AsNoTracking()
            .Where(d =>
                d.Kind == ShowtimeKind.Event
                && d.BusinessDate >= period.From
                && d.BusinessDate <= period.To
                && d.Showtime.Status != ShowtimeStatus.Cancelled
            );

    private static IReadOnlyDictionary<string, object?> ToRow(
        string? movie,
        string? showtime,
        string? room,
        Row r
    ) =>
        new Dictionary<string, object?>
        {
            ["movie"] = movie,
            ["showtime"] = showtime,
            ["room"] = room,
            ["tickets"] = r.Tickets,
            ["revenue"] = r.Revenue,
            ["occupancy"] = ReportMath.Percent(r.Tickets, r.Sellable),
            ["giftsGiven"] = r.GiftsGiven,
        };
}
