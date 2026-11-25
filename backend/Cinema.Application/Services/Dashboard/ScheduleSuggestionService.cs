using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.Dashboard;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Interfaces.Showtimes;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Dashboard;

public class ScheduleSuggestionService(
    IAppDbContext db,
    IAppCache cache,
    IScheduleAnalyticsService analytics
) : IScheduleSuggestionService
{
    private const int MaxTargets = 3;

    private readonly record struct Agg(int Count, int Sellable, int Sold)
    {
        public decimal Percent =>
            Sellable > 0
                ? Math.Round((decimal)Sold * 100m / Sellable, 2, MidpointRounding.AwayFromZero)
                : 0m;

        public static Agg operator +(Agg a, Agg b) =>
            new(a.Count + b.Count, a.Sellable + b.Sellable, a.Sold + b.Sold);
    }

    private sealed record RoomInfo(int RoomId, string RoomName, int Seats);

    private sealed record Candidate(RoomInfo Room, string Slot, Agg Stat, int Rank);

    public async Task<ScheduleSuggestionsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var high = settings.SuggestionHighOccupancyPercent;
        var low = settings.SuggestionLowOccupancyPercent;
        var minShowtimes = settings.SuggestionMinShowtimes;

        var report = await analytics.GetReportAsync(null, null, cancellationToken);
        var days = report.To.DayNumber - report.From.DayNumber + 1;

        var movieTitles = new Dictionary<int, string?>();
        var movieAgg = new Dictionary<int, Agg>();
        var movieRoomAgg = new Dictionary<(int Movie, int Room), Agg>();
        var roomSlotAgg = new Dictionary<(int Room, string Slot), Agg>();
        var movieRoomSlots = new HashSet<(int Movie, int Room, string Slot)>();
        foreach (var row in report.Items)
        {
            var agg = new Agg(row.ShowtimeCount, row.SellableSeats, row.TicketsSold);
            movieTitles[row.MovieId] = row.MovieTitle;
            movieAgg[row.MovieId] = movieAgg.GetValueOrDefault(row.MovieId) + agg;
            movieRoomAgg[(row.MovieId, row.RoomId)] =
                movieRoomAgg.GetValueOrDefault((row.MovieId, row.RoomId)) + agg;
            roomSlotAgg[(row.RoomId, row.TimeSlot)] =
                roomSlotAgg.GetValueOrDefault((row.RoomId, row.TimeSlot)) + agg;
            movieRoomSlots.Add((row.MovieId, row.RoomId, row.TimeSlot));
        }

        var rooms = await db
            .CinemaRooms.AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => new RoomInfo(
                r.RoomId,
                r.RoomName,
                r.Seats.Count(s => s.IsActive && !s.IsAisle)
            ))
            .ToListAsync(cancellationToken);

        var periodStartUtc = BusinessDay.StartUtc(
            report.From,
            settings.OpeningTime,
            settings.ClosingTime
        );
        var periodEndUtc = BusinessDay.EndUtc(
            report.To,
            settings.OpeningTime,
            settings.ClosingTime
        );
        var closedRoomIds = (
            await db
                .RoomClosures.AsNoTracking()
                .Where(c => c.EndTime > periodStartUtc && c.StartTime < periodEndUtc)
                .Select(c => c.RoomId)
                .Distinct()
                .ToListAsync(cancellationToken)
        ).ToHashSet();

        var usableRooms = rooms
            .Where(r => r.Seats > 0 && !closedRoomIds.Contains(r.RoomId))
            .ToList();
        var roomById = rooms.ToDictionary(r => r.RoomId);
        var slots = TimeSlots
            .All.Where(s => Operates(s, settings.OpeningTime, settings.ClosingTime))
            .ToList();

        var movieIds = movieAgg.Keys.ToList();
        HashSet<int> nowShowing =
            movieIds.Count == 0
                ? []
                : (
                    await db
                        .Movies.AsNoTracking()
                        .Where(m =>
                            movieIds.Contains(m.MovieId) && m.Status == MovieStatus.NowShowing
                        )
                        .Select(m => m.MovieId)
                        .ToListAsync(cancellationToken)
                ).ToHashSet();

        var eligible = movieAgg
            .Where(kv => kv.Value.Count >= minShowtimes && nowShowing.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var hot = eligible
            .Where(kv => kv.Value.Percent >= high)
            .OrderByDescending(kv => kv.Value.Percent)
            .ThenByDescending(kv => kv.Value.Sold)
            .ToList();
        var cold = eligible
            .Where(kv => kv.Value.Percent <= low)
            .OrderBy(kv => kv.Value.Percent)
            .ToList();

        var items = new List<ScheduleSuggestionDto>();

        foreach (var (movieId, stat) in hot)
        {
            var need = AvgTickets(stat);
            var targets = usableRooms
                .Where(r => r.Seats >= need)
                .SelectMany(r => slots.Select(s => (Room: r, Slot: s)))
                .Select(x =>
                {
                    var slotStat = roomSlotAgg.GetValueOrDefault((x.Room.RoomId, x.Slot));
                    var rank =
                        slotStat.Count == 0 ? 0
                        : slotStat.Percent <= low ? 1
                        : slotStat.Count < minShowtimes ? 2
                        : -1;
                    return new Candidate(x.Room, x.Slot, slotStat, rank);
                })
                .Where(c => c.Rank >= 0)
                .OrderBy(c => c.Rank)
                .ThenBy(c => c.Stat.Percent)
                .ThenByDescending(c => c.Room.Seats)
                .Take(MaxTargets)
                .ToList();

            if (targets.Count == 0)
            {
                items.Add(
                    Build(
                        ScheduleSuggestionType.HighDemand,
                        ScheduleSuggestionAction.AddShowtimes,
                        movieId,
                        movieTitles,
                        null,
                        null,
                        stat,
                        null,
                        string.Format(
                            Messages.Schedule_AddShowtimes_NoSlot,
                            movieTitles[movieId],
                            Pct(stat.Percent),
                            stat.Count,
                            days,
                            high
                        )
                    )
                );
                continue;
            }

            foreach (var t in targets)
            {
                var slotLabel = SlotLabel(t.Slot);
                var reason = t.Rank switch
                {
                    0 => string.Format(
                        Messages.Schedule_AddShowtimes_EmptySlot,
                        movieTitles[movieId],
                        Pct(stat.Percent),
                        stat.Count,
                        days,
                        high,
                        t.Room.RoomName,
                        slotLabel
                    ),
                    1 => string.Format(
                        Messages.Schedule_AddShowtimes_LowSlot,
                        movieTitles[movieId],
                        Pct(stat.Percent),
                        stat.Count,
                        days,
                        high,
                        t.Room.RoomName,
                        slotLabel,
                        Pct(t.Stat.Percent),
                        t.Stat.Count,
                        low
                    ),
                    _ => string.Format(
                        Messages.Schedule_AddShowtimes_FewSlot,
                        movieTitles[movieId],
                        Pct(stat.Percent),
                        stat.Count,
                        days,
                        high,
                        t.Room.RoomName,
                        slotLabel,
                        t.Stat.Count,
                        minShowtimes
                    ),
                };
                items.Add(
                    Build(
                        ScheduleSuggestionType.HighDemand,
                        ScheduleSuggestionAction.AddShowtimes,
                        movieId,
                        movieTitles,
                        t.Room,
                        t.Slot,
                        stat,
                        t.Stat,
                        reason
                    )
                );
            }
        }

        foreach (var (movieId, stat) in cold)
        {
            var title = movieTitles[movieId];
            items.Add(
                Build(
                    ScheduleSuggestionType.LowDemand,
                    ScheduleSuggestionAction.ReduceShowtimes,
                    movieId,
                    movieTitles,
                    null,
                    null,
                    stat,
                    null,
                    string.Format(
                        Messages.Schedule_ReduceShowtimes,
                        title,
                        Pct(stat.Percent),
                        stat.Count,
                        days,
                        low
                    )
                )
            );

            var need = AvgTickets(stat);
            var primary = movieRoomAgg
                .Where(kv => kv.Key.Movie == movieId)
                .OrderByDescending(kv => kv.Value.Count)
                .Select(kv => kv.Key.Room)
                .FirstOrDefault();
            if (roomById.TryGetValue(primary, out var fromRoom))
            {
                var primaryStat = movieRoomAgg[(movieId, primary)];
                var primaryNeed = AvgTickets(primaryStat);
                var smaller = usableRooms
                    .Where(r => r.Seats < fromRoom.Seats && r.Seats >= primaryNeed)
                    .OrderBy(r => r.Seats)
                    .FirstOrDefault();
                if (smaller is not null)
                    items.Add(
                        Build(
                            ScheduleSuggestionType.LowDemand,
                            ScheduleSuggestionAction.MoveToSmallerRoom,
                            movieId,
                            movieTitles,
                            smaller,
                            null,
                            stat,
                            null,
                            string.Format(
                                Messages.Schedule_MoveToSmallerRoom,
                                title,
                                Pct(stat.Percent),
                                stat.Count,
                                primaryNeed,
                                fromRoom.RoomName,
                                fromRoom.Seats,
                                smaller.RoomName,
                                smaller.Seats
                            ),
                            fromRoom
                        )
                    );
            }

            var busier = usableRooms
                .Where(r => r.Seats >= need)
                .SelectMany(r => slots.Select(s => (Room: r, Slot: s)))
                .Where(x => !movieRoomSlots.Contains((movieId, x.Room.RoomId, x.Slot)))
                .Select(x => new Candidate(
                    x.Room,
                    x.Slot,
                    roomSlotAgg.GetValueOrDefault((x.Room.RoomId, x.Slot)),
                    0
                ))
                .Where(c => c.Stat.Count >= minShowtimes && c.Stat.Percent > stat.Percent)
                .OrderByDescending(c => c.Stat.Percent)
                .ThenByDescending(c => c.Room.Seats)
                .FirstOrDefault();
            if (busier is not null)
                items.Add(
                    Build(
                        ScheduleSuggestionType.LowDemand,
                        ScheduleSuggestionAction.MoveToBusierSlot,
                        movieId,
                        movieTitles,
                        busier.Room,
                        busier.Slot,
                        stat,
                        busier.Stat,
                        string.Format(
                            Messages.Schedule_MoveToBusierSlot,
                            title,
                            Pct(stat.Percent),
                            stat.Count,
                            busier.Room.RoomName,
                            SlotLabel(busier.Slot),
                            Pct(busier.Stat.Percent),
                            busier.Stat.Count
                        )
                    )
                );
        }

        foreach (var room in usableRooms.OrderBy(r => r.RoomName))
        {
            foreach (var slot in slots)
            {
                var slotStat = roomSlotAgg.GetValueOrDefault((room.RoomId, slot));
                var isEmpty = slotStat.Count == 0;
                if (!isEmpty && slotStat.Percent > low)
                    continue;

                var movies = hot.Where(h =>
                        room.Seats >= AvgTickets(h.Value)
                        && !movieRoomSlots.Contains((h.Key, room.RoomId, slot))
                    )
                    .Take(MaxTargets);
                foreach (var (movieId, stat) in movies)
                {
                    var reason = isEmpty
                        ? string.Format(
                            Messages.Schedule_FillEmptySlot_Empty,
                            room.RoomName,
                            SlotLabel(slot),
                            days,
                            movieTitles[movieId],
                            Pct(stat.Percent),
                            stat.Count,
                            high
                        )
                        : string.Format(
                            Messages.Schedule_FillEmptySlot_Low,
                            room.RoomName,
                            SlotLabel(slot),
                            Pct(slotStat.Percent),
                            slotStat.Count,
                            days,
                            low,
                            movieTitles[movieId],
                            Pct(stat.Percent),
                            stat.Count,
                            high
                        );
                    items.Add(
                        Build(
                            ScheduleSuggestionType.EmptySlot,
                            ScheduleSuggestionAction.FillEmptySlot,
                            movieId,
                            movieTitles,
                            room,
                            slot,
                            stat,
                            slotStat,
                            reason
                        )
                    );
                }
            }
        }

        return new ScheduleSuggestionsDto(report.From, report.To, high, low, minShowtimes, items);
    }

    private static ScheduleSuggestionDto Build(
        ScheduleSuggestionType type,
        ScheduleSuggestionAction action,
        int movieId,
        Dictionary<int, string?> titles,
        RoomInfo? room,
        string? slot,
        Agg movie,
        Agg? target,
        string reason,
        RoomInfo? fromRoom = null
    ) =>
        new(
            MakeId(type, action, movieId, room?.RoomId, slot),
            type,
            action,
            movieId,
            titles.GetValueOrDefault(movieId),
            room?.RoomId,
            room?.RoomName,
            slot,
            fromRoom?.RoomId,
            fromRoom?.RoomName,
            movie.Count,
            movie.Percent,
            target?.Count,
            target?.Percent,
            reason
        );

    private static string MakeId(
        ScheduleSuggestionType type,
        ScheduleSuggestionAction action,
        int movieId,
        int? roomId,
        string? slot
    )
    {
        var raw = $"{type}|{action}|{movieId}|{roomId}|{slot}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16].ToLowerInvariant();
    }

    private static int AvgTickets(Agg stat) =>
        stat.Count == 0 ? 0 : (int)Math.Ceiling((double)stat.Sold / stat.Count);

    private static string Pct(decimal value) =>
        value.ToString("0.#", CultureInfo.InvariantCulture);

    private static string SlotLabel(string slot) =>
        slot switch
        {
            TimeSlots.Morning => Messages.Schedule_Slot_Morning,
            TimeSlots.Afternoon => Messages.Schedule_Slot_Afternoon,
            TimeSlots.Evening => Messages.Schedule_Slot_Evening,
            _ => Messages.Schedule_Slot_Late,
        };

    private static bool Operates(string slot, TimeOnly? opening, TimeOnly? closing)
    {
        if (opening is not { } open || closing is not { } close)
            return true;

        var (start, end) = slot switch
        {
            TimeSlots.Morning => (0, TimeSlots.AfternoonStartHour * 60),
            TimeSlots.Afternoon => (TimeSlots.AfternoonStartHour * 60, TimeSlots.EveningStartHour * 60),
            TimeSlots.Evening => (TimeSlots.EveningStartHour * 60, TimeSlots.LateStartHour * 60),
            _ => (TimeSlots.LateStartHour * 60, 24 * 60),
        };
        var o = open.Hour * 60 + open.Minute;
        var c = close.Hour * 60 + close.Minute;
        if (o == c)
            return true;
        return o < c ? start < c && end > o : start < c || end > o;
    }
}
