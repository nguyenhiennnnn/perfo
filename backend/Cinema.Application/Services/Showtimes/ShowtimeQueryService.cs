using Cinema.Application.Caching;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Interfaces.Showtimes;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeQueryService(
    IAppDbContext db,
    IPricingService pricingService,
    IAppCache cache
) : IShowtimeQueryService
{
    public async Task<ShowtimeSeatsResponseDto> GetShowtimeSeatsAsync(
        int showtimeId,
        int? viewerUserId,
        CancellationToken cancellationToken
    )
    {
        var showtime =
            await db
                .Showtimes.Include(s => s.Room)
                    .ThenInclude(r => r.ScreenType)
                .Include(s => s.Movie)
                    .ThenInclude(m => m.AgeRating)
                .FirstOrDefaultAsync(
                    s => s.ShowtimeId == showtimeId && s.Kind != ShowtimeKind.Private,
                    cancellationToken
                ) ?? throw new NotFoundException(Messages.NotFound_Showtime);

        var seatRows = await db
            .ShowtimeSeats.Where(ss => ss.ShowtimeId == showtimeId)
            .OrderBy(ss => ss.Seat.SeatRow)
            .ThenBy(ss => ss.Seat.SeatNumber)
            .Select(ss => new
            {
                ss.ShowtimeSeatId,
                ss.SeatId,
                ss.Seat.SeatRow,
                ss.Seat.SeatNumber,
                ss.Seat.SeatTypeId,
                SeatTypeName = ss.Seat.SeatType != null ? ss.Seat.SeatType.Name : null,
                ss.Seat.CoupleId,
                ss.Status,
                ss.HeldByOrderId,
                IsCouple = ss.Seat.SeatType != null && ss.Seat.SeatType.IsCouple,
            })
            .ToListAsync(cancellationToken);

        var myOrder = await FindViewerHoldOrderAsync(showtimeId, viewerUserId, cancellationToken);

        var ticketTypes = await cache.GetAsync(CacheKeys.TicketTypes, cancellationToken);
        var pricedAt = DateTime.UtcNow;
        var priceRequests = seatRows
            .SelectMany(r =>
                new int?[] { null }
                    .Concat(ticketTypes.Select(t => (int?)t.TicketTypeId))
                    .Select(typeId => new SeatPriceRequest(r.SeatId, typeId))
            )
            .ToList();
        var quotes = await pricingService.QuoteBatchAsync(
            showtimeId,
            priceRequests,
            pricedAt,
            cancellationToken
        );
        var quotesPerSeat = ticketTypes.Count + 1;

        var seats = seatRows
            .Select(
                (r, index) =>
                {
                    var seatQuotes = quotes
                        .Skip(index * quotesPerSeat)
                        .Take(quotesPerSeat)
                        .ToList();
                    return new ShowtimeSeatItemDto(
                        r.ShowtimeSeatId,
                        r.SeatRow,
                        r.SeatNumber,
                        r.SeatTypeId,
                        r.SeatTypeName,
                        r.CoupleId,
                        r.Status.ToDtoStatus(),
                        seatQuotes[0].ListPrice,
                        r.IsCouple,
                        myOrder is not null
                            && r.Status == ShowtimeSeatStatus.Held
                            && r.HeldByOrderId == myOrder.OrderId,
                        [
                            .. seatQuotes
                                .Skip(1)
                                .Select(q => new SeatTicketPriceDto(
                                    q.TicketTypeId!.Value,
                                    q.ListPrice
                                )),
                        ]
                    );
                }
            )
            .ToList();

        var ticketTypeItems = ticketTypes
            .Select(
                (t, index) =>
                {
                    var reference = quotes.Count > 0 ? quotes[index + 1] : null;
                    return new ShowtimeTicketTypeDto(
                        t.TicketTypeId,
                        t.Name,
                        t.PriceMultiplier,
                        t.RequiresCardVerification,
                        reference?.IsEligible ?? true,
                        reference?.IneligibleReason
                    );
                }
            )
            .ToList();

        var myHold = myOrder is null
            ? null
            : new ShowtimeMyHoldDto(
                myOrder.OrderId,
                myOrder.OrderCode,
                myOrder.ExpiresAt,
                seats.Where(s => s.HeldByMe).Select(s => s.ShowtimeSeatId).ToList()
            );

        var aisles = await db
            .Seats.Where(s => s.RoomId == showtime.RoomId && s.IsAisle)
            .Select(s => new AislePositionDto(s.SeatRow, s.SeatNumber))
            .ToListAsync(cancellationToken);

        var maxSeatRowIdx =
            seats.Count > 0
                ? seats.Max(s => s.SeatRow.Length > 0 ? (s.SeatRow[0] - 'A' + 1) : 0)
                : 0;
        var maxAisleRowIdx =
            aisles.Count > 0
                ? aisles.Max(a => a.SeatRow.Length > 0 ? (a.SeatRow[0] - 'A' + 1) : 0)
                : 0;
        var actualMaxRow = Math.Max(maxSeatRowIdx, maxAisleRowIdx);
        var effectiveRows = actualMaxRow > 0 ? actualMaxRow : showtime.Room.Rows;

        var maxSeatCol = seats.Count > 0 ? seats.Max(s => s.SeatNumber) : 0;
        var maxAisleCol = aisles.Count > 0 ? aisles.Max(a => a.SeatNumber) : 0;
        var actualMaxCol = Math.Max(maxSeatCol, maxAisleCol);
        var effectiveCols = actualMaxCol > 0 ? actualMaxCol : showtime.Room.Cols;

        return new ShowtimeSeatsResponseDto(
            showtimeId,
            showtime.Room.RoomName,
            showtime.Movie.Title,
            showtime.StartTime,
            effectiveRows,
            effectiveCols,
            aisles,
            seats,
            showtime.Movie.PosterUrl,
            showtime.Movie.AgeRating?.Code,
            showtime.Room.ScreenType?.Name,
            myHold is { ShowtimeSeatIds.Count: > 0 } ? myHold : null,
            ticketTypeItems,
            pricedAt
        );
    }

    private async Task<ViewerHoldOrder?> FindViewerHoldOrderAsync(
        int showtimeId,
        int? viewerUserId,
        CancellationToken cancellationToken
    )
    {
        if (viewerUserId is null)
            return null;

        var now = DateTime.UtcNow;
        return await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.UserId == viewerUserId
                && o.Channel == SalesChannel.Online
                && o.Status == OrderStatus.Pending
                && o.ExpiresAt > now
                && o.Lines.Any(l => l.ShowtimeId == showtimeId)
            )
            .OrderByDescending(o => o.OrderId)
            .Select(o => new ViewerHoldOrder(o.OrderId, o.OrderCode, o.ExpiresAt!.Value))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed record ViewerHoldOrder(int OrderId, string OrderCode, DateTime ExpiresAt);

    public async Task<List<SeatSuggestionDto>> GetSeatSuggestionsAsync(
        int showtimeId,
        int count,
        CancellationToken cancellationToken
    )
    {
        var roomCols =
            await db
                .Showtimes.Where(s =>
                    s.ShowtimeId == showtimeId && s.Kind != ShowtimeKind.Private
                )
                .Select(s => (int?)s.Room.Cols)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);

        var seats = await db.LoadSeatSlotsAsync(showtimeId, cancellationToken);
        var cols = Math.Max(roomCols, seats.Count > 0 ? seats.Max(s => s.Number) : 1);
        var suggestions = SeatLayoutRules.Suggest(seats, cols, count);
        if (suggestions.Count == 0)
            return suggestions;

        var suggestedIds = suggestions.SelectMany(s => s.ShowtimeSeatIds).Distinct().ToList();
        var seatIdByShowtimeSeat = await db
            .ShowtimeSeats.Where(ss =>
                ss.ShowtimeId == showtimeId && suggestedIds.Contains(ss.ShowtimeSeatId)
            )
            .ToDictionaryAsync(ss => ss.ShowtimeSeatId, ss => ss.SeatId, cancellationToken);
        var quotes = await pricingService.QuoteBatchAsync(
            showtimeId,
            [.. seatIdByShowtimeSeat.Values.Select(seatId => new SeatPriceRequest(seatId, null))],
            DateTime.UtcNow,
            cancellationToken
        );
        var priceBySeat = quotes.ToDictionary(q => q.SeatId!.Value, q => q.ListPrice);

        return
        [
            .. suggestions.Select(s =>
                s with
                {
                    TotalPrice = s.ShowtimeSeatIds.Sum(id => priceBySeat[seatIdByShowtimeSeat[id]]),
                }
            ),
        ];
    }

    public async Task<WholeCinemaScheduleDto> GetWholeCinemaScheduleAsync(
        DateOnly date,
        string? screenType,
        CancellationToken cancellationToken
    )
    {
        var dayStart = BusinessTime.StartOfDayUtc(date);
        var dayEnd = BusinessTime.StartOfNextDayUtc(date);

        var dayQuery = db.Showtimes.Where(s =>
            s.Status == ShowtimeStatus.Scheduled
            && s.Kind != ShowtimeKind.Private
            && s.StartTime >= dayStart
            && s.StartTime < dayEnd
        );

        var formats = await dayQuery
            .Select(s => s.Room.ScreenType != null ? s.Room.ScreenType.Name : "2D")
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        var lookaheadDays = await db.LoadShowtimeLookaheadDaysAsync(cancellationToken);
        var maxDate = BusinessTime.LocalDate(DateTime.UtcNow.AddDays(lookaheadDays));
        if (date > maxDate)
            return new WholeCinemaScheduleDto(date.ToString(DateTimePatterns.Date), [], formats);

        if (!string.IsNullOrWhiteSpace(screenType))
            dayQuery = dayQuery.Where(s =>
                (s.Room.ScreenType != null ? s.Room.ScreenType.Name : "2D") == screenType
            );

        var showtimes = await dayQuery
            .Include(s => s.Room)
                .ThenInclude(r => r.ScreenType)
            .Include(s => s.Movie)
                .ThenInclude(m => m.AgeRating)
            .OrderBy(s => s.Movie.Title)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        var showtimeIds = showtimes.Select(s => s.ShowtimeId).ToList();

        var seatCounts = await db
            .ShowtimeSeats.WhereSellable()
            .Where(ss => showtimeIds.Contains(ss.ShowtimeId))
            .GroupBy(ss => ss.ShowtimeId)
            .Select(g => new
            {
                ShowtimeId = g.Key,
                Total = g.Count(),
                Available = g.Count(x => x.Status == ShowtimeSeatStatus.Available),
            })
            .ToListAsync(cancellationToken);
        var seatCountLookup = seatCounts.ToDictionary(x => x.ShowtimeId, x => x);
        var fromPrices = await pricingService.GetFromPricesAsync(
            showtimeIds,
            DateTime.UtcNow,
            cancellationToken
        );

        var movies = showtimes
            .GroupBy(s => s.Movie)
            .Select(g => new WholeCinemaMovieDto(
                g.Key.MovieId,
                g.Key.Title,
                g.Key.PosterUrl,
                g.Key.DurationMinutes,
                g.Key.AgeRating?.Code,
                g.Select(s =>
                    {
                        var counts = seatCountLookup.GetValueOrDefault(s.ShowtimeId);
                        var slot = s.Adapt<WholeCinemaShowtimeSlotDto>();
                        return slot with
                        {
                            AvailableSeats = counts?.Available ?? 0,
                            TotalSeats = counts?.Total ?? 0,
                            FromPrice = fromPrices.GetValueOrDefault(s.ShowtimeId)?.FromPrice,
                        };
                    })
                    .ToList()
            ))
            .ToList();

        return new WholeCinemaScheduleDto(date.ToString(DateTimePatterns.Date), movies, formats);
    }

    public async Task<ShowtimeCalendarResponseDto> GetShowtimesCalendarAsync(
        AdminShowtimeCalendarFilter filter,
        CancellationToken cancellationToken
    )
    {
        var rangeStart = BusinessTime.StartOfDayUtc(filter.From);
        var rangeEndExclusive = BusinessTime.StartOfNextDayUtc(filter.To);

        var query = db
            .Showtimes.Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r.ScreenType)
            .Where(s => s.StartTime >= rangeStart && s.StartTime < rangeEndExclusive);

        if (filter.RoomId is not null)
            query = query.Where(s => s.RoomId == filter.RoomId);
        if (filter.MovieId is not null)
            query = query.Where(s => s.MovieId == filter.MovieId);
        if (
            !string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<ShowtimeStatus>(filter.Status, out var status)
        )
            query = query.Where(s => s.Status == status);

        var showtimes = await query
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.Room.RoomName)
            .ToListAsync(cancellationToken);

        var showtimeIds = showtimes.Select(s => s.ShowtimeId).ToList();

        var seatCounts = await db
            .ShowtimeSeats.WhereSellable()
            .Where(ss => showtimeIds.Contains(ss.ShowtimeId))
            .GroupBy(ss => ss.ShowtimeId)
            .Select(g => new
            {
                ShowtimeId = g.Key,
                Total = g.Count(),
                Available = g.Count(x => x.Status == ShowtimeSeatStatus.Available),
            })
            .ToListAsync(cancellationToken);
        var seatCountLookup = seatCounts.ToDictionary(x => x.ShowtimeId, x => x);
        var fromPrices = await pricingService.GetFromPricesAsync(
            showtimeIds,
            DateTime.UtcNow,
            cancellationToken
        );

        var days = showtimes
            .GroupBy(s => BusinessTime.LocalDate(s.StartTime))
            .OrderBy(g => g.Key)
            .Select(g => new ShowtimeCalendarDayDto(
                g.Key.ToString(DateTimePatterns.Date),
                g.Select(s =>
                    {
                        var counts = seatCountLookup.GetValueOrDefault(s.ShowtimeId);
                        var item = s.Adapt<ShowtimeCalendarItemDto>();
                        return item with
                        {
                            AvailableSeats = counts?.Available ?? 0,
                            TotalSeats = counts?.Total ?? 0,
                            FromPrice = fromPrices.GetValueOrDefault(s.ShowtimeId)?.FromPrice,
                        };
                    })
                    .ToList()
            ))
            .ToList();

        var closureQuery = db.RoomClosures.AsNoTracking().Where(c =>
            c.EndTime > rangeStart && c.StartTime < rangeEndExclusive
        );
        if (filter.RoomId is not null)
            closureQuery = closureQuery.Where(c => c.RoomId == filter.RoomId);
        var closures = await closureQuery
            .OrderBy(c => c.StartTime)
            .ThenBy(c => c.RoomClosureId)
            .Select(c => new ShowtimeCalendarClosureDto(
                c.RoomClosureId,
                c.RoomId,
                c.Room.RoomName,
                c.StartTime,
                c.EndTime,
                c.Reason
            ))
            .ToListAsync(cancellationToken);

        return new ShowtimeCalendarResponseDto(
            filter.From.ToString(DateTimePatterns.Date),
            filter.To.ToString(DateTimePatterns.Date),
            days,
            closures
        );
    }
}
