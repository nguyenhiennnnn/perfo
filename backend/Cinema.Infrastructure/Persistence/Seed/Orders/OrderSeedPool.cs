using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Persistence.Seed.Orders;

internal enum SeedWhen
{
    Past,
    Upcoming,
    Tomorrow,
    Soon,
    Cancelled,
}

internal sealed record SeedShowtime(
    int ShowtimeId,
    string? ScreenType,
    DateTime StartTime,
    DateTime EndTime,
    ShowtimeStatus Status
);

internal sealed record SeedSeatAllocation(SeedShowtime Showtime, IReadOnlyList<ShowtimeSeat> Seats);

internal sealed class OrderSeedPool
{
    private readonly IReadOnlyList<SeedShowtime> _past;
    private readonly IReadOnlyList<SeedShowtime> _upcoming;
    private readonly IReadOnlyList<SeedShowtime> _tomorrow;
    private readonly IReadOnlyList<SeedShowtime> _soon;
    private readonly IReadOnlyList<SeedShowtime> _cancelled;
    private readonly HashSet<int> _usedSeatIds = [];
    private readonly Dictionary<int, int> _usage = [];

    private OrderSeedPool(IReadOnlyList<SeedShowtime> rows, DateTime now, int cancelDeadlineHours)
    {
        var leadCutoff = now.AddHours(cancelDeadlineHours + 1);
        var tomorrowStart = BusinessTime.StartOfDayUtc(ShowtimeScheduler.LocalDay(now, 1));

        _past =
        [
            .. rows
                .Where(s => s.Status != ShowtimeStatus.Cancelled && s.EndTime < now.AddMinutes(-5))
                .OrderByDescending(s => s.StartTime),
        ];
        _upcoming =
        [
            .. rows
                .Where(s => s.Status == ShowtimeStatus.Scheduled && s.StartTime > leadCutoff)
                .OrderBy(s => s.StartTime),
        ];
        _tomorrow = [.. _upcoming.Where(s => s.StartTime >= tomorrowStart)];
        _soon =
        [
            .. rows
                .Where(s =>
                    s.Status == ShowtimeStatus.Scheduled
                    && s.StartTime > now.AddMinutes(45)
                    && s.StartTime <= now.AddHours(10)
                )
                .OrderBy(s => s.StartTime),
        ];
        _cancelled =
        [
            .. rows
                .Where(s => s.Status == ShowtimeStatus.Cancelled && s.StartTime > now)
                .OrderBy(s => s.StartTime),
        ];
    }

    public static async Task<OrderSeedPool> LoadAsync(
        AppDbContext db,
        DateTime now,
        int cancelDeadlineHours,
        CancellationToken cancellationToken
    )
    {
        var from = now.AddDays(-ShowtimeScheduler.PastDays - 1);
        var to = now.AddDays(ShowtimeScheduler.PastDays + 1);
        var rows = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.StartTime >= from && s.StartTime <= to)
            .Select(s => new SeedShowtime(
                s.ShowtimeId,
                s.Room.ScreenType != null ? s.Room.ScreenType.Name : null,
                s.StartTime,
                s.EndTime,
                s.Status
            ))
            .ToListAsync(cancellationToken);

        return new OrderSeedPool(rows, now, cancelDeadlineHours);
    }

    public async Task<SeedSeatAllocation?> AllocateAsync(
        AppDbContext db,
        SeedWhen when,
        string? screenType,
        int seatCount,
        CancellationToken cancellationToken
    )
    {
        var candidates = Candidates(when)
            .OrderBy(s => screenType is not null && s.ScreenType == screenType ? 0 : 1)
            .ThenBy(s => _usage.GetValueOrDefault(s.ShowtimeId))
            .ToList();

        foreach (var showtime in candidates)
        {
            var available = await db
                .ShowtimeSeats.Include(ss => ss.Seat)
                .Where(ss =>
                    ss.ShowtimeId == showtime.ShowtimeId
                    && ss.Status == ShowtimeSeatStatus.Available
                )
                .OrderBy(ss => ss.Seat.SeatRow)
                .ThenBy(ss => ss.Seat.SeatNumber)
                .ToListAsync(cancellationToken);
            available.RemoveAll(ss => _usedSeatIds.Contains(ss.ShowtimeSeatId));

            var block = PickBlock(available, seatCount);
            if (block is null)
                continue;

            foreach (var seat in block)
                _usedSeatIds.Add(seat.ShowtimeSeatId);
            _usage[showtime.ShowtimeId] = _usage.GetValueOrDefault(showtime.ShowtimeId) + 1;
            return new SeedSeatAllocation(showtime, block);
        }

        return null;
    }

    private IReadOnlyList<SeedShowtime> Candidates(SeedWhen when) =>
        when switch
        {
            SeedWhen.Past => _past,
            SeedWhen.Tomorrow => _tomorrow.Count > 0 ? _tomorrow : _upcoming,
            SeedWhen.Soon => _soon.Count > 0 ? _soon : _upcoming,
            SeedWhen.Cancelled => _cancelled,
            _ => _upcoming,
        };

    private static List<ShowtimeSeat>? PickBlock(List<ShowtimeSeat> available, int count)
    {
        if (count <= 0 || available.Count < count)
            return null;

        var rows = available
            .GroupBy(s => s.Seat.SeatRow)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();
        var middle = (rows.Count - 1) / 2.0;
        var ordered = rows
            .Select((group, index) => (Group: group, Distance: Math.Abs(index - middle)))
            .OrderBy(x => x.Distance)
            .Select(x => x.Group);

        foreach (var row in ordered)
        {
            var seats = row.OrderBy(s => s.Seat.SeatNumber).ToList();
            var runStart = 0;
            for (var i = 1; i <= seats.Count; i++)
            {
                var broken =
                    i == seats.Count
                    || seats[i].Seat.SeatNumber != seats[i - 1].Seat.SeatNumber + 1;
                if (!broken)
                    continue;

                var length = i - runStart;
                if (length >= count)
                    return seats.GetRange(runStart + (length - count) / 2, count);
                runStart = i;
            }
        }

        return available.Take(count).ToList();
    }
}
