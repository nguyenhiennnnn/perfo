using Cinema.Application.Caching;
using Cinema.Application.DTOs.Dashboard;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Dashboard;

public class SeatHeatmapService(IAppDbContext db, IAppCache cache) : ISeatHeatmapService
{
    private const int MaxDays = 90;

    public async Task<SeatHeatmapDto> GetAsync(
        int roomId,
        DateOnly from,
        DateOnly to,
        int? movieId,
        int? genreId,
        CancellationToken cancellationToken
    )
    {
        if (from == default || to == default || from > to)
            throw new BusinessRuleException(Messages.Rule_OccupancyRangeInvalid);
        if (to.DayNumber - from.DayNumber + 1 > MaxDays)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_OccupancyRangeTooLong, MaxDays)
            );

        var roomName =
            await db
                .CinemaRooms.AsNoTracking()
                .Where(r => r.RoomId == roomId)
                .Select(r => r.RoomName)
                .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException(Messages.NotFound_Room);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var startUtc = BusinessDay.StartUtc(from, settings.OpeningTime, settings.ClosingTime);
        var endUtc = BusinessDay.EndUtc(to, settings.OpeningTime, settings.ClosingTime);

        var showtimes = db
            .Showtimes.AsNoTracking()
            .Where(s =>
                s.RoomId == roomId
                && s.StartTime >= startUtc
                && s.StartTime < endUtc
                && s.Status != ShowtimeStatus.Cancelled
                && !db.ShowtimeDailyStats.Any(d =>
                    d.ShowtimeId == s.ShowtimeId && d.Kind != ShowtimeKind.Regular
                )
            );

        if (movieId is { } mid)
            showtimes = showtimes.Where(s => s.MovieId == mid);
        if (genreId is { } gid)
            showtimes = showtimes.Where(s =>
                db.MovieGenres.Any(mg => mg.MovieId == s.MovieId && mg.GenreId == gid)
            );

        var showtimeIds = showtimes.Select(s => s.ShowtimeId);

        var sold = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.Order.Status == OrderStatus.Confirmed
                && l.RefundedQuantity < l.Quantity
                && l.ShowtimeSeatId != null
                && l.ShowtimeId != null
                && showtimeIds.Contains(l.ShowtimeId.Value)
            )
            .GroupBy(l => l.ShowtimeSeat!.SeatId)
            .Select(g => new { SeatId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeatId, x => x.Count, cancellationToken);

        var today = BusinessDay.Of(DateTime.UtcNow, settings.OpeningTime, settings.ClosingTime);
        var eventTo = to < today ? to : today;
        var abandoned = new Dictionary<int, int>();
        if (from <= eventTo)
        {
            abandoned = await db
                .SeatHoldEvents.AsNoTracking()
                .Where(e =>
                    e.RoomId == roomId
                    && e.BusinessDate >= from
                    && e.BusinessDate <= eventTo
                    && (e.Type == SeatHoldEventType.Released || e.Type == SeatHoldEventType.Expired)
                    && showtimeIds.Contains(e.ShowtimeId)
                )
                .GroupBy(e => e.SeatId)
                .Select(g => new { SeatId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SeatId, x => x.Count, cancellationToken);
        }

        var seats = await db
            .Seats.AsNoTracking()
            .Where(s => s.RoomId == roomId && !s.IsAisle)
            .OrderBy(s => s.SeatRow)
            .ThenBy(s => s.SeatNumber)
            .Select(s => new
            {
                s.SeatId,
                s.SeatRow,
                s.SeatNumber,
                s.SeatTypeId,
                SeatTypeName = s.SeatType != null ? s.SeatType.Name : null,
                s.IsActive,
                s.CoupleId,
            })
            .ToListAsync(cancellationToken);

        var items = seats
            .Select(s => new SeatHeatmapSeatDto(
                s.SeatId,
                s.SeatRow,
                s.SeatNumber,
                s.SeatTypeId,
                s.SeatTypeName,
                s.IsActive,
                s.CoupleId,
                s.CoupleId != null,
                sold.GetValueOrDefault(s.SeatId),
                abandoned.GetValueOrDefault(s.SeatId)
            ))
            .ToList();

        return new SeatHeatmapDto(
            roomId,
            roomName,
            from,
            to,
            items.Count == 0 ? 0 : items.Max(i => i.TimesSold),
            items.Count == 0 ? 0 : items.Max(i => i.TimesAbandoned),
            items
        );
    }
}
