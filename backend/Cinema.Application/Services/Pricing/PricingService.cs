using Cinema.Application.Caching;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Interfaces.Reports;
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

    public async Task<IReadOnlyDictionary<int, ShowtimeFromPriceDto>> GetFromPricesAsync(
        IReadOnlyCollection<int> showtimeIds,
        DateTime atUtc,
        CancellationToken cancellationToken
    )
    {
        var ids = showtimeIds.Distinct().ToList();
        var result = new Dictionary<int, ShowtimeFromPriceDto>(ids.Count);
        if (ids.Count == 0)
            return result;

        var rows = await db
            .Showtimes.AsNoTracking()
            .Where(s => ids.Contains(s.ShowtimeId))
            .Select(s => new ShowtimeRow(
                s,
                s.Movie.Title,
                s.Movie.AgeRatingId,
                s.Room.RoomName,
                s.Room.ScreenTypeId
            ))
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return result;

        var seatTypePairs = await db
            .ShowtimeSeats.AsNoTracking()
            .Where(ss => ids.Contains(ss.ShowtimeId))
            .Select(ss => new { ss.ShowtimeId, ss.Seat.SeatTypeId })
            .Distinct()
            .ToListAsync(cancellationToken);
        var seatTypesByShowtime = seatTypePairs
            .GroupBy(p => p.ShowtimeId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.SeatTypeId).ToList());
        var multipliers = await LoadSeatTypeMultipliersAsync(
            seatTypePairs.Select(p => p.SeatTypeId),
            cancellationToken
        );

        var shared = await LoadSharedAsync(cancellationToken);
        var resolved = rows.Where(r => seatTypesByShowtime.ContainsKey(r.Showtime.ShowtimeId))
            .Select(r =>
            {
                var scope = scopeResolver.Resolve(r.Showtime);
                return (Row: r, Scope: scope, Candidates: ResolveCandidates(scope, shared));
            })
            .ToList();

        var occupancyIds = resolved
            .Where(x => x.Candidates.Any(c => c.RuleType == PricingRuleType.Occupancy))
            .Select(x => x.Row.Showtime.ShowtimeId)
            .ToList();
        var occupancy = await LoadOccupancyAsync(occupancyIds, atUtc, cancellationToken);

        foreach (var item in resolved)
        {
            var showtimeId = item.Row.Showtime.ShowtimeId;
            var context = await BuildContextAsync(
                item.Row,
                item.Scope,
                item.Candidates,
                shared,
                atUtc,
                occupancy.GetValueOrDefault(showtimeId) ?? EmptyOccupancy,
                cancellationToken
            );
            var seatTypeIds = seatTypesByShowtime[showtimeId];

            var defaultPrice = seatTypeIds.Min(seatTypeId =>
                BuildQuote(
                    context,
                    seatTypeId,
                    SeatMultiplier(multipliers, seatTypeId),
                    null
                ).ListPrice
            );

            var byTicketType = new List<TicketTypeFromPriceDto>();
            foreach (var ticketType in context.TicketTypes)
            {
                var quotes = seatTypeIds
                    .Select(seatTypeId =>
                        BuildQuote(
                            context,
                            seatTypeId,
                            SeatMultiplier(multipliers, seatTypeId),
                            ticketType
                        )
                    )
                    .ToList();
                if (!quotes[0].IsEligible)
                    continue;

                byTicketType.Add(
                    new TicketTypeFromPriceDto(
                        ticketType.TicketTypeId,
                        ticketType.Name,
                        quotes.Min(q => q.ListPrice)
                    )
                );
            }

            var fromPrice = byTicketType.Count > 0
                ? Math.Min(defaultPrice, byTicketType.Min(t => t.FromPrice))
                : defaultPrice;
            result[showtimeId] = new ShowtimeFromPriceDto(showtimeId, fromPrice, byTicketType);
        }

        return result;
    }

    private async Task<PricingShared> LoadSharedAsync(CancellationToken cancellationToken) =>
        new(
            await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken),
            await cache.GetAsync(CacheKeys.TicketTypes, cancellationToken),
            await cache.GetAsync(CacheKeys.PricingRules, cancellationToken)
        );

    private static IReadOnlyList<PricingRuleSnapshot> ResolveCandidates(
        PricingScope scope,
        PricingShared shared
    ) =>
        scope.Mode switch
        {
            ShowtimePricingMode.Standard => shared.StandardRules,
            ShowtimePricingMode.Event => scope.EventRules,
            _ => [],
        };

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

        var shared = await LoadSharedAsync(cancellationToken);
        var scope = scopeResolver.Resolve(row.Showtime);
        var candidates = ResolveCandidates(scope, shared);

        var occupancy =
            forceOccupancy || candidates.Any(r => r.RuleType == PricingRuleType.Occupancy)
                ? (await LoadOccupancyAsync([showtimeId], atUtc, cancellationToken))[showtimeId]
                : EmptyOccupancy;

        return await BuildContextAsync(
            row,
            scope,
            candidates,
            shared,
            atUtc,
            occupancy,
            cancellationToken
        );
    }

    private async Task<PricingContext> BuildContextAsync(
        ShowtimeRow row,
        PricingScope scope,
        IReadOnlyList<PricingRuleSnapshot> candidates,
        PricingShared shared,
        DateTime atUtc,
        OccupancySnapshot occupancy,
        CancellationToken cancellationToken
    )
    {
        var showtime = row.Showtime;
        var localStart = BusinessTime.ToLocal(showtime.StartTime);
        var businessDate = PricingCalendar.BusinessDate(
            localStart,
            shared.Settings.OpeningTime,
            shared.Settings.ClosingTime
        );

        IReadOnlyList<SpecialDaySnapshot> specialDays = candidates.Any(r =>
            r.RuleType == PricingRuleType.SpecialDay
        )
            ? await cache.GetAsync(CacheKeys.HolidayCalendar, cancellationToken)
            : [];

        var localTime = TimeOnly.FromDateTime(localStart);
        IReadOnlyList<AppliedRule> rules = showtime.Kind == ShowtimeKind.Private
            ? []
            : PricingRuleMatcher.Match(
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
            PricingParameters.From(shared.Settings),
            rules,
            occupancy,
            shared.TicketTypes
        );
    }

    private async Task<Dictionary<int, OccupancySnapshot>> LoadOccupancyAsync(
        IReadOnlyCollection<int> showtimeIds,
        DateTime atUtc,
        CancellationToken cancellationToken
    )
    {
        var result = new Dictionary<int, OccupancySnapshot>(showtimeIds.Count);
        if (showtimeIds.Count == 0)
            return result;

        var ids = showtimeIds.ToList();
        var now = DateTime.UtcNow;
        var counts = await db
            .ShowtimeSeats.AsNoTracking()
            .WhereSellable()
            .Where(ss => ids.Contains(ss.ShowtimeId))
            .GroupBy(ss => ss.ShowtimeId)
            .Select(g => new
            {
                ShowtimeId = g.Key,
                Total = g.Count(),
                Booked = g.Count(ss => ss.Status == ShowtimeSeatStatus.Booked),
                Held = g.Count(ss =>
                    ss.Status == ShowtimeSeatStatus.Held
                    && (ss.HeldUntil == null || ss.HeldUntil > now)
                ),
            })
            .ToListAsync(cancellationToken);
        var countById = counts.ToDictionary(c => c.ShowtimeId);

        foreach (var id in ids)
        {
            var velocity = await salesVelocity.GetTicketsPerHourAsync(id, atUtc, cancellationToken);
            var count = countById.GetValueOrDefault(id);
            result[id] = new OccupancySnapshot(
                count?.Total ?? 0,
                count?.Booked ?? 0,
                count?.Held ?? 0,
                velocity
            );
        }

        return result;
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

    private static readonly OccupancySnapshot EmptyOccupancy = new(0, 0, 0, null);

    private sealed record SeatTypeInfo(int? SeatTypeId, string Name, decimal PriceMultiplier);

    private sealed record PricingShared(
        CinemaSettingsSnapshot Settings,
        IReadOnlyList<TicketTypeSnapshot> TicketTypes,
        IReadOnlyList<PricingRuleSnapshot> StandardRules
    );

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
