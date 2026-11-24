using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Utils;

public static class SeatHoldEventRecorder
{
    public static void Stage(
        IAppDbContext db,
        IEnumerable<ShowtimeSeat> seats,
        Showtime showtime,
        SeatHoldEventType type,
        DateTime occurredAt,
        TimeOnly? opening,
        TimeOnly? closing
    )
    {
        var businessDate = BusinessDay.Of(showtime.StartTime, opening, closing);
        db.SeatHoldEvents.AddRange(
            seats.Select(s => new SeatHoldEvent
            {
                ShowtimeId = showtime.ShowtimeId,
                SeatId = s.SeatId,
                RoomId = showtime.RoomId,
                MovieId = showtime.MovieId,
                BusinessDate = businessDate,
                Type = type,
                OccurredAt = occurredAt,
            })
        );
    }

    public static async Task StageAsync(
        IAppDbContext db,
        IReadOnlyCollection<int> showtimeSeatIds,
        SeatHoldEventType type,
        DateTime occurredAt,
        CancellationToken cancellationToken
    )
    {
        if (showtimeSeatIds.Count == 0)
            return;

        var settings = await db.LoadCinemaSettingsOrDefaultAsync(cancellationToken);
        var rows = await db
            .ShowtimeSeats.AsNoTracking()
            .Where(ss => showtimeSeatIds.Contains(ss.ShowtimeSeatId))
            .Select(ss => new
            {
                ss.ShowtimeId,
                ss.SeatId,
                ss.Showtime.RoomId,
                ss.Showtime.MovieId,
                ss.Showtime.StartTime,
            })
            .ToListAsync(cancellationToken);

        db.SeatHoldEvents.AddRange(
            rows.Select(r => new SeatHoldEvent
            {
                ShowtimeId = r.ShowtimeId,
                SeatId = r.SeatId,
                RoomId = r.RoomId,
                MovieId = r.MovieId,
                BusinessDate = BusinessDay.Of(r.StartTime, settings.OpeningTime, settings.ClosingTime),
                Type = type,
                OccurredAt = occurredAt,
            })
        );
    }
}
