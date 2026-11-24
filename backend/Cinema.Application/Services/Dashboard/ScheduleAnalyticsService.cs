using Cinema.Application.DTOs.Dashboard;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Dashboard;

public class ScheduleAnalyticsService(IAppDbContext db) : IScheduleAnalyticsService
{
    private const int MaxDays = 366;

    private sealed record Row(
        int MovieId,
        int RoomId,
        int Hour,
        int ShowtimeCount,
        int SellableSeats,
        int TicketsSold,
        decimal NetRevenue,
        double SeatMinutes
    );

    private sealed class Acc
    {
        public int ShowtimeCount;
        public int SellableSeats;
        public int TicketsSold;
        public decimal NetRevenue;
        public double SeatMinutes;
    }

    public async Task<ScheduleReportDto> GetReportAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken
    )
    {
        var settings = await db.LoadCinemaSettingsOrDefaultAsync(cancellationToken);
        DateOnly effectiveTo =
            to ?? BusinessDay.Of(DateTime.UtcNow, settings.OpeningTime, settings.ClosingTime);
        DateOnly effectiveFrom =
            from ?? effectiveTo.AddDays(-(Math.Max(1, settings.SuggestionLookbackDays) - 1));

        if (effectiveFrom > effectiveTo)
            throw new BusinessRuleException(Messages.Rule_OccupancyRangeInvalid);
        if (effectiveTo.DayNumber - effectiveFrom.DayNumber + 1 > MaxDays)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_OccupancyRangeTooLong, MaxDays)
            );

        var offsetMinutes = (int)BusinessTime.Zone.BaseUtcOffset.TotalMinutes;

        List<Row> rows = await db
            .ShowtimeDailyStats.AsNoTracking()
            .Where(x =>
                x.BusinessDate >= effectiveFrom
                && x.BusinessDate <= effectiveTo
                && x.Kind == ShowtimeKind.Regular
                && x.SellableSeats > 0
                && x.Showtime.Status != ShowtimeStatus.Cancelled
            )
            .GroupBy(x => new
            {
                x.Showtime.MovieId,
                x.Showtime.RoomId,
                Hour = x.Showtime.StartTime.AddMinutes(offsetMinutes).Hour,
            })
            .Select(g => new Row(
                g.Key.MovieId,
                g.Key.RoomId,
                g.Key.Hour,
                g.Count(),
                g.Sum(x => x.SellableSeats),
                g.Sum(x => x.TicketsSold),
                g.Sum(x =>
                    x.GrossTicketRevenue - x.DiscountAmount - x.RefundAmount + x.ComboRevenue
                ),
                g.Sum(x =>
                    x.SellableSeats * (x.Showtime.EndTime - x.Showtime.StartTime).TotalMinutes
                )
            ))
            .ToListAsync(cancellationToken);

        var merged = new Dictionary<(int MovieId, int RoomId, string Slot), Acc>();
        foreach (var row in rows)
        {
            var key = (row.MovieId, row.RoomId, TimeSlots.FromLocalHour(row.Hour));
            if (!merged.TryGetValue(key, out var acc))
                merged[key] = acc = new Acc();
            acc.ShowtimeCount += row.ShowtimeCount;
            acc.SellableSeats += row.SellableSeats;
            acc.TicketsSold += row.TicketsSold;
            acc.NetRevenue += row.NetRevenue;
            acc.SeatMinutes += row.SeatMinutes;
        }

        var movieIds = merged.Keys.Select(k => k.MovieId).Distinct().ToList();
        var roomIds = merged.Keys.Select(k => k.RoomId).Distinct().ToList();
        var movies = movieIds.Count == 0
            ? new Dictionary<int, string>()
            : await db
                .Movies.AsNoTracking()
                .Where(m => movieIds.Contains(m.MovieId))
                .ToDictionaryAsync(m => m.MovieId, m => m.Title, cancellationToken);
        var rooms = roomIds.Count == 0
            ? new Dictionary<int, string>()
            : await db
                .CinemaRooms.AsNoTracking()
                .Where(r => roomIds.Contains(r.RoomId))
                .ToDictionaryAsync(r => r.RoomId, r => r.RoomName, cancellationToken);

        var items = merged
            .Select(kv =>
            {
                var (movieId, roomId, slot) = kv.Key;
                var a = kv.Value;
                var seatHours = (decimal)a.SeatMinutes / 60m;
                return new ScheduleReportRowDto(
                    movieId,
                    movies.GetValueOrDefault(movieId),
                    roomId,
                    rooms.GetValueOrDefault(roomId),
                    slot,
                    a.ShowtimeCount,
                    a.SellableSeats,
                    a.TicketsSold,
                    a.SellableSeats > 0
                        ? Math.Round(
                            (decimal)a.TicketsSold / a.SellableSeats * 100m,
                            2,
                            MidpointRounding.AwayFromZero
                        )
                        : null,
                    a.NetRevenue,
                    Math.Round(
                        a.NetRevenue / a.ShowtimeCount,
                        0,
                        MidpointRounding.AwayFromZero
                    ),
                    seatHours > 0
                        ? Math.Round(a.NetRevenue / seatHours, 0, MidpointRounding.AwayFromZero)
                        : null
                );
            })
            .OrderBy(x => x.MovieTitle)
            .ThenBy(x => x.RoomName)
            .ThenBy(x => TimeSlots.All.ToList().IndexOf(x.TimeSlot))
            .ToList();

        return new ScheduleReportDto(effectiveFrom, effectiveTo, items);
    }
}
