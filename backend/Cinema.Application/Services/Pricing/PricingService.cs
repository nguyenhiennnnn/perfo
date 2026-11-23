using Cinema.Application.Caching;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Pricing;

public class PricingService(
    IAppDbContext db,
    IAppCache cache,
    IShowtimePricingScopeResolver scopeResolver,
    ISalesVelocityProvider salesVelocity
) : IPricingService
{
    public async Task<PriceQuoteDto> QuoteAsync(
        int showtimeId,
        int seatId,
        int? ticketTypeId,
        DateTime atUtc,
        CancellationToken cancellationToken
    ) =>
        (
            await QuoteBatchAsync(
                showtimeId,
                [new SeatPriceRequest(seatId, ticketTypeId)],
                atUtc,
                cancellationToken
            )
        )[0];

    public async Task<IReadOnlyList<PriceQuoteDto>> QuoteBatchAsync(
        int showtimeId,
        IReadOnlyCollection<SeatPriceRequest> seats,
        DateTime atUtc,
        CancellationToken cancellationToken
    )
    {
        if (seats.Count == 0)
            return [];

        var context = await LoadContextAsync(showtimeId, atUtc, false, cancellationToken);
        var seatIds = seats.Select(s => s.SeatId).Distinct().ToList();
        var seatTypeBySeat = await db
            .Seats.AsNoTracking()
            .Where(s => seatIds.Contains(s.SeatId) && s.RoomId == context.Showtime.RoomId)
            .Select(s => new { s.SeatId, s.SeatTypeId })
            .ToDictionaryAsync(s => s.SeatId, s => s.SeatTypeId, cancellationToken);
        if (seatTypeBySeat.Count != seatIds.Count)
            throw new NotFoundException(Messages.NotFound_SeatInShowtime);

        var multipliers = await LoadSeatTypeMultipliersAsync(
            seatTypeBySeat.Values,
            cancellationToken
        );
        var ticketTypes = context.TicketTypes.ToDictionary(t => t.TicketTypeId);

        var cached = new Dictionary<(int?, int?), PriceQuoteDto>();
        var quotes = new List<PriceQuoteDto>(seats.Count);
        foreach (var request in seats)
        {
            TicketTypeSnapshot? ticketType = null;
            if (
                request.TicketTypeId is { } ticketTypeId
                && !ticketTypes.TryGetValue(ticketTypeId, out ticketType)
            )
                throw new NotFoundException(Messages.NotFound_TicketType);

            var seatTypeId = seatTypeBySeat[request.SeatId];
            if (!cached.TryGetValue((seatTypeId, request.TicketTypeId), out var quote))
            {
                quote = BuildQuote(
                    context,
                    seatTypeId,
                    SeatMultiplier(multipliers, seatTypeId),
                    ticketType
                );
                cached[(seatTypeId, request.TicketTypeId)] = quote;
            }

            quotes.Add(quote with { SeatId = request.SeatId });
        }

        return quotes;
    }

    public async Task<ShowtimePriceTableDto> GetPriceTableAsync(
        int showtimeId,
        DateTime atUtc,
        CancellationToken cancellationToken
    )
    {
        var context = await LoadContextAsync(showtimeId, atUtc, true, cancellationToken);

        var seatTypeIds = await db
            .ShowtimeSeats.AsNoTracking()
            .Where(ss => ss.ShowtimeId == showtimeId)
            .Select(ss => ss.Seat.SeatTypeId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var typedIds = seatTypeIds.OfType<int>().ToList();
        var seatTypes = await db
            .SeatTypes.AsNoTracking()
            .Where(st => typedIds.Contains(st.SeatTypeId))
            .Select(st => new SeatTypeInfo(st.SeatTypeId, st.Name, st.PriceMultiplier))
            .ToListAsync(cancellationToken);
        if (seatTypeIds.Contains(null))
            seatTypes.Add(new SeatTypeInfo(null, Messages.Pricing_SeatType_Default, 1m));

        var rows = seatTypes
            .OrderBy(st => st.PriceMultiplier)
            .ThenBy(st => st.SeatTypeId)
            .Select(st => new SeatTypePriceRowDto(
                st.SeatTypeId,
                st.Name,
                st.PriceMultiplier,
                context
                    .TicketTypes.Select(t =>
                        BuildQuote(context, st.SeatTypeId, st.PriceMultiplier, t)
                    )
                    .ToList()
            ))
            .ToList();

        var showtime = context.Showtime;
        return new ShowtimePriceTableDto(
            showtime.ShowtimeId,
            showtime.MovieId,
            context.MovieTitle,
            showtime.RoomId,
            context.RoomName,
            showtime.StartTime,
            showtime.BasePrice,
            context.AtUtc,
            context.Mode.ToString(),
            new PricingOccupancyDto(
                context.Occupancy.TotalSeats,
                context.Occupancy.BookedSeats,
                context.Occupancy.HeldSeats,
                context.Occupancy.Percent,
                context.Occupancy.SalesPerHour
            ),
            [
                .. context.Rules.Select(a => new AppliedPricingRuleDto(
                    a.Rule.PricingRuleId,
                    a.Rule.Name,
                    a.Rule.RuleType.ToString(),
                    a.Rule.AdjustmentMode.ToString(),
                    a.Value,
                    a.Rule.Priority,
                    a.Reason
                )),
            ],
            [
                .. context.TicketTypes.Select(t => new PricingTicketTypeDto(
                    t.TicketTypeId,
                    t.Name,
                    t.PriceMultiplier,
                    t.RequiresCardVerification
                )),
            ],
            rows
        );
    }

    private async Task<PricingContext> LoadContextAsync(
        int showtimeId,
        DateTime atUtc,
        bool forceOccupancy,
        CancellationToken cancellationToken
    )
    {
        var row =
            await db
                .Showtimes.AsNoTracking()
                .Where(s => s.ShowtimeId == showtimeId)
                .Select(s => new ShowtimeRow(
                    s,
                    s.Movie.Title,
                    s.Movie.AgeRatingId,
                    s.Room.RoomName,
                    s.Room.ScreenTypeId
                ))
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);
        var showtime = row.Showtime;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var ticketTypes = await cache.GetAsync(CacheKeys.TicketTypes, cancellationToken);

        var scope = scopeResolver.Resolve(showtime);
        IReadOnlyList<PricingRuleSnapshot> candidates = scope.Mode switch
        {
            ShowtimePricingMode.Standard => await cache.GetAsync(
                CacheKeys.PricingRules,
                cancellationToken
            ),
            ShowtimePricingMode.Event => scope.EventRules,
            _ => [],
        };

        var localStart = BusinessTime.ToLocal(showtime.StartTime);
        var businessDate = PricingCalendar.BusinessDate(
            localStart,
            settings.OpeningTime,
            settings.ClosingTime
        );

        var occupancy =
            forceOccupancy || candidates.Any(r => r.RuleType == PricingRuleType.Occupancy)
                ? await LoadOccupancyAsync(showtimeId, atUtc, cancellationToken)
                : new OccupancySnapshot(0, 0, 0, null);
        IReadOnlyList<SpecialDaySnapshot> specialDays = candidates.Any(r =>
            r.RuleType == PricingRuleType.SpecialDay
        )
            ? await cache.GetAsync(CacheKeys.HolidayCalendar, cancellationToken)
            : [];

        var localTime = TimeOnly.FromDateTime(localStart);
        var rules = PricingRuleMatcher.Match(
            candidates,
            new RuleMatchContext(
                showtime.RoomId,
                row.ScreenTypeId,
                showtime.MovieId,
                businessDate,
                localTime,
                Math.Max(0m, (decimal)(showtime.StartTime - atUtc).TotalHours),
                occupancy.Percent,
                specialDays
            )
        );

        return new PricingContext(
            showtime,
            row.MovieTitle,
            row.RoomName,
            row.AgeRatingId,
            atUtc,
            scope.Mode,
            businessDate,
            localTime,
            PricingParameters.From(settings),
            rules,
            occupancy,
            ticketTypes
        );
    }

    private async Task<OccupancySnapshot> LoadOccupancyAsync(
        int showtimeId,
        DateTime atUtc,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var counts = await db
            .ShowtimeSeats.AsNoTracking()
            .Where(ss => ss.ShowtimeId == showtimeId)
            .GroupBy(ss => ss.ShowtimeId)
            .Select(g => new
            {
                Total = g.Count(),
                Booked = g.Count(ss => ss.Status == ShowtimeSeatStatus.Booked),
                Held = g.Count(ss =>
                    ss.Status == ShowtimeSeatStatus.Held
                    && (ss.HeldUntil == null || ss.HeldUntil > now)
                ),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var velocity = await salesVelocity.GetTicketsPerHourAsync(
            showtimeId,
            atUtc,
            cancellationToken
        );
        return new OccupancySnapshot(
            counts?.Total ?? 0,
            counts?.Booked ?? 0,
            counts?.Held ?? 0,
            velocity
        );
    }

    private async Task<Dictionary<int, decimal>> LoadSeatTypeMultipliersAsync(
        IEnumerable<int?> seatTypeIds,
        CancellationToken cancellationToken
    )
    {
        var ids = seatTypeIds.OfType<int>().Distinct().ToList();
        return ids.Count == 0
            ? new Dictionary<int, decimal>()
            : await db
                .SeatTypes.AsNoTracking()
                .Where(st => ids.Contains(st.SeatTypeId))
                .ToDictionaryAsync(st => st.SeatTypeId, st => st.PriceMultiplier, cancellationToken);
    }

    private static decimal SeatMultiplier(Dictionary<int, decimal> multipliers, int? seatTypeId) =>
        seatTypeId is { } id && multipliers.TryGetValue(id, out var multiplier) ? multiplier : 1m;

    private static PriceQuoteDto BuildQuote(
        PricingContext context,
        int? seatTypeId,
        decimal seatMultiplier,
        TicketTypeSnapshot? ticketType
    )
    {
        var ticketMultiplier = ticketType?.PriceMultiplier ?? 1m;
        var breakdown = PriceCalculator.Calculate(
            context.Showtime.BasePrice,
            seatMultiplier,
            ticketMultiplier,
            context.Rules,
            context.Parameters
        );
        var ineligibleReason =
            ticketType is null
                ? null
                : TicketTypeEligibility.IneligibleReason(
                    ticketType,
                    context.AgeRatingId,
                    context.LocalTime,
                    context.BusinessDate
                );

        return new PriceQuoteDto(
            context.Showtime.ShowtimeId,
            null,
            seatTypeId,
            ticketType?.TicketTypeId,
            context.Showtime.BasePrice,
            seatMultiplier,
            ticketMultiplier,
            breakdown.ReferencePrice,
            breakdown.DynamicAdjustment,
            breakdown.ListPrice,
            ineligibleReason is null,
            ineligibleReason,
            breakdown.Adjustments
        );
    }

    private sealed record ShowtimeRow(
        Showtime Showtime,
        string MovieTitle,
        int? AgeRatingId,
        string RoomName,
        int? ScreenTypeId
    );

    private sealed record SeatTypeInfo(int? SeatTypeId, string Name, decimal PriceMultiplier);

    private sealed record PricingContext(
        Showtime Showtime,
        string MovieTitle,
        string RoomName,
        int? AgeRatingId,
        DateTime AtUtc,
        ShowtimePricingMode Mode,
        DateOnly BusinessDate,
        TimeOnly LocalTime,
        PricingParameters Parameters,
        IReadOnlyList<AppliedRule> Rules,
        OccupancySnapshot Occupancy,
        IReadOnlyList<TicketTypeSnapshot> TicketTypes
    );
}
