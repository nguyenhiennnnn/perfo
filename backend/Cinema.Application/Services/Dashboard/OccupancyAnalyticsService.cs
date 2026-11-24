using Cinema.Application.DTOs.Dashboard;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Dashboard;

public class OccupancyAnalyticsService(IAppDbContext db) : IOccupancyAnalyticsService
{
    private const int MaxDays = 366;

    private sealed record Row(
        int Key,
        bool IsCurrent,
        int ShowtimeCount,
        int SellableSeats,
        int TicketsSold
    );

    private readonly record struct Sums(int Showtimes, int Sellable, int Sold)
    {
        public static Sums operator +(Sums a, Sums b) =>
            new(a.Showtimes + b.Showtimes, a.Sellable + b.Sellable, a.Sold + b.Sold);
    }

    public async Task<OccupancyAnalyticsDto> GetAsync(
        DateOnly from,
        DateOnly to,
        OccupancyGroupBy groupBy,
        IReadOnlyCollection<ShowtimeKind>? kinds,
        CancellationToken cancellationToken
    )
    {
        if (from == default || to == default || from > to)
            throw new BusinessRuleException(Messages.Rule_OccupancyRangeInvalid);

        var totalDays = to.DayNumber - from.DayNumber + 1;
        if (totalDays > MaxDays)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_OccupancyRangeTooLong, MaxDays)
            );

        var kindList =
            kinds is { Count: > 0 } ? kinds.Distinct().ToList() : [ShowtimeKind.Regular];
        var previousTo = from.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(totalDays - 1));

        var rows = await QueryRowsAsync(
            from,
            to,
            previousFrom,
            kindList,
            groupBy,
            cancellationToken
        );

        var groups = new Dictionary<int, (Sums Current, Sums Previous)>();
        foreach (var row in rows)
        {
            var key = groupBy == OccupancyGroupBy.TimeSlot ? SlotIndex(row.Key) : row.Key;
            var sums = new Sums(row.ShowtimeCount, row.SellableSeats, row.TicketsSold);
            groups.TryGetValue(key, out var existing);
            groups[key] = row.IsCurrent
                ? (existing.Current + sums, existing.Previous)
                : (existing.Current, existing.Previous + sums);
        }

        var names = await LoadNamesAsync(groupBy, groups.Keys, cancellationToken);

        var items = groups
            .Select(g =>
            {
                var (key, name) = Describe(groupBy, g.Key, names);
                return (
                    Order: g.Key,
                    Name: name,
                    Dto: ToGroup(key, name, g.Value.Current, g.Value.Previous)
                );
            })
            .OrderBy(x => groupBy is OccupancyGroupBy.Movie or OccupancyGroupBy.Room ? 0 : x.Order)
            .ThenByDescending(x => groupBy == OccupancyGroupBy.Movie ? x.Dto.TicketsSold : 0)
            .ThenBy(x => groupBy is OccupancyGroupBy.Movie or OccupancyGroupBy.Room ? x.Name : null)
            .Select(x => x.Dto)
            .ToList();

        var currentTotal = groups.Values.Aggregate(default(Sums), (a, g) => a + g.Current);
        var previousTotal = groups.Values.Aggregate(default(Sums), (a, g) => a + g.Previous);

        var totalPercent = Percent(currentTotal);
        var previousPercent = Percent(previousTotal);

        return new OccupancyAnalyticsDto(
            from,
            to,
            previousFrom,
            previousTo,
            groupBy,
            kindList,
            new OccupancyTotalDto(
                currentTotal.Showtimes,
                currentTotal.Sellable,
                currentTotal.Sold,
                totalPercent,
                previousTotal.Showtimes,
                previousTotal.Sellable,
                previousTotal.Sold,
                previousPercent,
                Change(totalPercent, previousPercent)
            ),
            items
        );
    }

    private async Task<List<Row>> QueryRowsAsync(
        DateOnly from,
        DateOnly to,
        DateOnly previousFrom,
        List<ShowtimeKind> kinds,
        OccupancyGroupBy groupBy,
        CancellationToken cancellationToken
    )
    {
        var source = db
            .ShowtimeDailyStats.AsNoTracking()
            .Where(x =>
                x.BusinessDate >= previousFrom
                && x.BusinessDate <= to
                && kinds.Contains(x.Kind)
                && x.SellableSeats > 0
                && x.Showtime.Status != ShowtimeStatus.Cancelled
            );

        switch (groupBy)
        {
            case OccupancyGroupBy.Room:
                return await source
                    .GroupBy(x => new { x.Showtime.RoomId, IsCurrent = x.BusinessDate >= from })
                    .Select(g => new Row(
                        g.Key.RoomId,
                        g.Key.IsCurrent,
                        g.Count(),
                        g.Sum(x => x.SellableSeats),
                        g.Sum(x => x.TicketsSold)
                    ))
                    .ToListAsync(cancellationToken);

            case OccupancyGroupBy.Movie:
                return await source
                    .GroupBy(x => new { x.Showtime.MovieId, IsCurrent = x.BusinessDate >= from })
                    .Select(g => new Row(
                        g.Key.MovieId,
                        g.Key.IsCurrent,
                        g.Count(),
                        g.Sum(x => x.SellableSeats),
                        g.Sum(x => x.TicketsSold)
                    ))
                    .ToListAsync(cancellationToken);

            case OccupancyGroupBy.Weekday:
                return await source
                    .GroupBy(x => new
                    {
                        Day = (int)x.BusinessDate.DayOfWeek,
                        IsCurrent = x.BusinessDate >= from,
                    })
                    .Select(g => new Row(
                        g.Key.Day,
                        g.Key.IsCurrent,
                        g.Count(),
                        g.Sum(x => x.SellableSeats),
                        g.Sum(x => x.TicketsSold)
                    ))
                    .ToListAsync(cancellationToken);

            default:
                var offsetMinutes = (int)BusinessTime.Zone.BaseUtcOffset.TotalMinutes;
                return await source
                    .GroupBy(x => new
                    {
                        Hour = x.Showtime.StartTime.AddMinutes(offsetMinutes).Hour,
                        IsCurrent = x.BusinessDate >= from,
                    })
                    .Select(g => new Row(
                        g.Key.Hour,
                        g.Key.IsCurrent,
                        g.Count(),
                        g.Sum(x => x.SellableSeats),
                        g.Sum(x => x.TicketsSold)
                    ))
                    .ToListAsync(cancellationToken);
        }
    }

    private async Task<Dictionary<int, string>> LoadNamesAsync(
        OccupancyGroupBy groupBy,
        IEnumerable<int> ids,
        CancellationToken cancellationToken
    )
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return [];

        return groupBy switch
        {
            OccupancyGroupBy.Room => await db
                .CinemaRooms.AsNoTracking()
                .Where(r => idList.Contains(r.RoomId))
                .ToDictionaryAsync(r => r.RoomId, r => r.RoomName, cancellationToken),
            OccupancyGroupBy.Movie => await db
                .Movies.AsNoTracking()
                .Where(m => idList.Contains(m.MovieId))
                .ToDictionaryAsync(m => m.MovieId, m => m.Title, cancellationToken),
            _ => [],
        };
    }

    private static (string Key, string? Name) Describe(
        OccupancyGroupBy groupBy,
        int key,
        Dictionary<int, string> names
    ) =>
        groupBy switch
        {
            OccupancyGroupBy.TimeSlot => (TimeSlots.All[key], null),
            OccupancyGroupBy.Weekday => (((DayOfWeek)key).ToString(), null),
            _ => (key.ToString(), names.GetValueOrDefault(key)),
        };

    private static int SlotIndex(int localHour) =>
        TimeSlots.All.ToList().IndexOf(TimeSlots.FromLocalHour(localHour));

    private static OccupancyGroupDto ToGroup(string key, string? name, Sums current, Sums previous)
    {
        var percent = Percent(current);
        var previousPercent = Percent(previous);
        return new OccupancyGroupDto(
            key,
            name,
            current.Showtimes,
            current.Sellable,
            current.Sold,
            percent,
            previous.Showtimes,
            previous.Sellable,
            previous.Sold,
            previousPercent,
            Change(percent, previousPercent)
        );
    }

    private static decimal? Percent(Sums sums) =>
        sums.Sellable > 0
            ? Math.Round(
                (decimal)sums.Sold / sums.Sellable * 100m,
                2,
                MidpointRounding.AwayFromZero
            )
            : null;

    private static decimal? Change(decimal? current, decimal? previous) =>
        current is { } c && previous is { } p ? c - p : null;
}
