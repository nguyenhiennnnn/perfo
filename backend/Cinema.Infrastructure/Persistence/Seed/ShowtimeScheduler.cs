using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Persistence.Seed;

internal readonly record struct ShowtimeSlot(int RoomId, DateTime Start, DateTime End);

internal readonly record struct PlanWindow(DateTime? NotBefore = null, DateTime? NotAfter = null);

internal sealed class ShowtimePlan
{
    public required IReadOnlyList<Movie> Movies { get; init; }
    public required IReadOnlyList<CinemaRoom> Rooms { get; init; }
    public required IReadOnlyDictionary<int, List<Seat>> SeatsByRoom { get; init; }
    public required TimeOnly Opening { get; init; }
    public required TimeOnly Closing { get; init; }
    public required int BufferMinutes { get; init; }
    public required double MinLeadHours { get; init; }
    public required int LookaheadDays { get; init; }

    public List<ShowtimeSlot> Busy { get; } = [];
    public HashSet<(int RoomId, DateTime Start)> Taken { get; } = [];

    public bool CanSchedule => Movies.Count > 0 && Rooms.Count > 0;

    public void Register(Showtime showtime)
    {
        Taken.Add((showtime.RoomId, showtime.StartTime));
        if (showtime.Status != ShowtimeStatus.Cancelled)
            Busy.Add(new ShowtimeSlot(showtime.RoomId, showtime.StartTime, showtime.EndTime));
    }

    public bool IsFree(int roomId, DateTime start, DateTime end) =>
        !Taken.Contains((roomId, start))
        && !Busy.Any(b => b.RoomId == roomId && b.Start < end && b.End > start);
}

internal static class ShowtimeScheduler
{
    public const int PastDays = 7;

    private const int MaxShowsPerRoomDay = 5;
    private const int MinShowsPerRoomDay = 3;
    private const int StepMinutes = 15;
    private const int DefaultCancelDeadlineHours = 2;
    private const double SafetyBufferHours = 0.5;

    public static int MinShowsPerDay => MinShowsPerRoomDay;

    public static int MaxShowsPerDay => MaxShowsPerRoomDay;

    public static DateOnly LocalDay(DateTime now, int offsetDays = 0) =>
        BusinessTime.LocalDate(now).AddDays(offsetDays);

    public static DateTime LeadCutoff(ShowtimePlan plan, DateTime now) =>
        now.AddHours(plan.MinLeadHours);

    public static async Task<ShowtimePlan> LoadAsync(
        AppDbContext db,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var settings = await db
            .CinemaSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        var lookaheadDays =
            settings?.ShowtimeLookaheadDays ?? CinemaSettings.DefaultShowtimeLookaheadDays;

        var movies = await db
            .Movies.AsNoTracking()
            .Where(m => m.Status == MovieStatus.NowShowing)
            .OrderBy(m => m.MovieId)
            .ToListAsync(cancellationToken);

        var rooms = await db
            .CinemaRooms.AsNoTracking()
            .Include(r => r.ScreenType)
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoomId)
            .ToListAsync(cancellationToken);

        var seatsByRoom = (
            await db
                .Seats.AsNoTracking()
                .Include(s => s.SeatType)
                .Where(s => s.IsActive && !s.IsAisle)
                .OrderBy(s => s.SeatId)
                .ToListAsync(cancellationToken)
        )
            .GroupBy(s => s.RoomId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var plan = new ShowtimePlan
        {
            Movies = movies,
            Rooms = rooms.Where(r => seatsByRoom.ContainsKey(r.RoomId)).ToList(),
            SeatsByRoom = seatsByRoom,
            Opening = settings?.OpeningTime ?? new TimeOnly(8, 0),
            Closing = settings?.ClosingTime ?? new TimeOnly(23, 59),
            BufferMinutes = settings?.RoomBufferMinutes ?? CinemaSettings.DefaultRoomBufferMinutes,
            MinLeadHours =
                (settings?.CancelDeadlineHours ?? DefaultCancelDeadlineHours) + SafetyBufferHours,
            LookaheadDays = lookaheadDays,
        };

        var from = BusinessTime.StartOfDayUtc(LocalDay(now, -PastDays - 1));
        var to = BusinessTime.StartOfDayUtc(LocalDay(now, lookaheadDays + 2));
        var rows = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.EndTime > from && s.StartTime < to)
            .Select(s => new
            {
                s.RoomId,
                s.StartTime,
                s.EndTime,
                s.Status,
            })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            plan.Taken.Add((row.RoomId, row.StartTime));
            if (row.Status != ShowtimeStatus.Cancelled)
                plan.Busy.Add(new ShowtimeSlot(row.RoomId, row.StartTime, row.EndTime));
        }

        return plan;
    }

    public static List<Showtime> PlanDay(
        ShowtimePlan plan,
        DateOnly localDay,
        IReadOnlyList<Movie> pool,
        PlanWindow window,
        int skipWhenAtLeast,
        ref int rotation,
        DateTime now
    )
    {
        var result = new List<Showtime>();
        if (pool.Count == 0)
            return result;

        var dayStart = BusinessTime.StartOfDayUtc(localDay);
        var dayEnd = BusinessTime.StartOfNextDayUtc(localDay);
        var close = BusinessTime.ToUtc(
            plan.Closing <= plan.Opening ? localDay.AddDays(1) : localDay,
            plan.Closing
        );

        for (var roomIndex = 0; roomIndex < plan.Rooms.Count; roomIndex++)
        {
            var room = plan.Rooms[roomIndex];
            var existing = plan.Busy.Count(b =>
                b.RoomId == room.RoomId && b.Start >= dayStart && b.Start < dayEnd
            );
            if (existing >= skipWhenAtLeast)
                continue;

            var toAdd = MaxShowsPerRoomDay - existing;
            var cursor = BusinessTime.ToUtc(localDay, plan.Opening).AddMinutes(60 + roomIndex * 15);
            if (window.NotBefore is { } floor && cursor < floor)
                cursor = floor;

            var added = 0;
            var guard = 0;
            while (added < toAdd && guard++ < 80)
            {
                var movie = pool[rotation % pool.Count];
                var start = RoundUp(cursor);
                var runEnd = start.AddMinutes(movie.DurationMinutes);
                if (runEnd > close)
                    break;

                var end = runEnd.AddMinutes(plan.BufferMinutes);
                if (window.NotAfter is { } ceiling && end >= ceiling)
                    break;

                var clashEnd = plan
                    .Busy.Where(b => b.RoomId == room.RoomId && b.Start < end && b.End > start)
                    .Select(b => (DateTime?)b.End)
                    .Max();
                if (clashEnd is { } blockedUntil)
                {
                    cursor = blockedUntil;
                    continue;
                }

                if (plan.Taken.Contains((room.RoomId, start)))
                {
                    cursor = start.AddMinutes(StepMinutes);
                    continue;
                }

                var status = end < now ? ShowtimeStatus.Completed : ShowtimeStatus.Scheduled;
                var showtime = Build(plan, movie, room, start, end, status, now);
                plan.Register(showtime);
                result.Add(showtime);

                rotation++;
                cursor = end;
                added++;
            }
        }

        return result;
    }

    public static Showtime? PlanSpecial(
        ShowtimePlan plan,
        Movie movie,
        CinemaRoom? preferredRoom,
        DateTime start,
        ShowtimeStatus status,
        DateTime now
    )
    {
        var end = start.AddMinutes(movie.DurationMinutes + plan.BufferMinutes);

        var room = plan
            .Rooms.OrderBy(r => r.RoomId == preferredRoom?.RoomId ? 0 : 1)
            .ThenBy(r => r.RoomId)
            .FirstOrDefault(r => plan.IsFree(r.RoomId, start, end));
        if (room is null)
            return null;

        var showtime = Build(plan, movie, room, start, end, status, now);
        plan.Register(showtime);
        return showtime;
    }

    private static DateTime RoundUp(DateTime value)
    {
        var step = TimeSpan.FromMinutes(StepMinutes).Ticks;
        var ticks = (value.Ticks + step - 1) / step * step;
        return new DateTime(ticks, value.Kind);
    }

    private static decimal GetBasePrice(string? screenTypeName, DateTime startUtc)
    {
        var local = BusinessTime.ToLocal(startUtc);
        var weekend = local.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday;
        var prime = local.Hour >= 17;

        var standard2D = weekend
            ? (prime ? 105_000m : 95_000m)
            : (prime ? 90_000m : 75_000m);

        return screenTypeName switch
        {
            "IMAX" => weekend ? 180_000m : 140_000m,
            "3D" => standard2D + (weekend ? 50_000m : 30_000m),
            _ => standard2D,
        };
    }

    private static Showtime Build(
        ShowtimePlan plan,
        Movie movie,
        CinemaRoom room,
        DateTime start,
        DateTime end,
        ShowtimeStatus status,
        DateTime now
    )
    {
        var basePrice = GetBasePrice(room.ScreenType?.Name, start);
        var showtime = new Showtime
        {
            MovieId = movie.MovieId,
            RoomId = room.RoomId,
            StartTime = start,
            EndTime = end,
            BasePrice = basePrice,
            Status = status,
            CreatedAt = start < now ? start.AddDays(-3) : now,
        };

        foreach (var seat in plan.SeatsByRoom[room.RoomId])
        {
            showtime.ShowtimeSeats.Add(
                new ShowtimeSeat
                {
                    SeatId = seat.SeatId,
                    Status = ShowtimeSeatStatus.Available,
                    Price = Math.Round(basePrice * (seat.SeatType?.PriceMultiplier ?? 1.0m), 0),
                }
            );
        }

        return showtime;
    }
}
