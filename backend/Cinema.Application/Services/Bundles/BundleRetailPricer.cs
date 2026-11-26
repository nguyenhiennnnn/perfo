using Cinema.Application.Caching;
using Cinema.Application.DTOs.Bundles;
using Cinema.Application.Interfaces.Bundles;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bundles;

public class BundleRetailPricer(IAppDbContext db, IAppCache cache, IPricingService pricingService)
    : IBundleRetailPricer
{
    public async Task<BundleRetailQuote?> QuoteAsync(
        BundleRetailInput input,
        CancellationToken cancellationToken
    )
    {
        var comboTotal = await ComboTotalAsync(input.Items, cancellationToken);
        var ticketItems = input.Items.Where(i => i.TicketTypeId is not null).ToList();
        if (ticketItems.Count == 0)
            return new BundleRetailQuote(comboTotal, null);

        var now = DateTime.UtcNow;
        var showtimeIds = await SelectSampleShowtimesAsync(input, now, cancellationToken);

        BundleRetailQuote? best = null;
        foreach (var showtimeId in showtimeIds)
        {
            var table = await pricingService.GetPriceTableAsync(showtimeId, now, cancellationToken);
            if (BundleRetailCalculator.TicketTotal(table, ticketItems, input.SeatTypeIds) is not { } ticketTotal)
                continue;

            var total = ticketTotal + comboTotal;
            if (best is null || total < best.RetailTotal)
                best = new BundleRetailQuote(total, showtimeId);
        }

        return best;
    }

    private async Task<decimal> ComboTotalAsync(
        IReadOnlyList<BundleItemSnapshot> items,
        CancellationToken cancellationToken
    )
    {
        var quantities = items
            .Where(i => i.ComboId is not null)
            .GroupBy(i => i.ComboId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        if (quantities.Count == 0)
            return 0m;

        var comboIds = quantities.Keys.ToList();
        var prices = await db
            .Combos.AsNoTracking()
            .Where(c => comboIds.Contains(c.ComboId))
            .ToDictionaryAsync(c => c.ComboId, c => c.Price, cancellationToken);

        return quantities.Sum(q => prices.GetValueOrDefault(q.Key) * q.Value);
    }

    private async Task<List<int>> SelectSampleShowtimesAsync(
        BundleRetailInput input,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var movieIds = input.MovieIds.ToList();
        var showtimeIds = input.ShowtimeIds.ToList();

        var query = db
            .Showtimes.AsNoTracking()
            .Where(s =>
                s.Kind == ShowtimeKind.Regular
                && s.Status == ShowtimeStatus.Scheduled
                && s.StartTime > now
            );
        if (movieIds.Count > 0)
            query = query.Where(s => movieIds.Contains(s.MovieId));
        if (showtimeIds.Count > 0)
            query = query.Where(s => showtimeIds.Contains(s.ShowtimeId));

        var candidates = await query
            .OrderBy(s => s.StartTime)
            .Take(BundleLimits.RetailCandidateShowtimes)
            .Select(s => new { s.ShowtimeId, s.RoomId, s.StartTime })
            .ToListAsync(cancellationToken);
        if (candidates.Count == 0)
            return [];

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var days = input.DaysOfWeek;

        return
        [
            .. candidates
                .Select(c => new
                {
                    c.ShowtimeId,
                    c.RoomId,
                    c.StartTime,
                    Local = BusinessTime.ToLocal(c.StartTime),
                })
                .Select(c => new
                {
                    c.ShowtimeId,
                    c.RoomId,
                    c.StartTime,
                    c.Local,
                    BusinessDate = PricingCalendar.BusinessDate(
                        c.Local,
                        settings.OpeningTime,
                        settings.ClosingTime
                    ),
                })
                .Where(c =>
                    (days.Count == 0 || days.Contains((int)c.BusinessDate.DayOfWeek))
                    && BundleRetailCalculator.InTimeWindow(
                        TimeOnly.FromDateTime(c.Local),
                        input.AllowedFromTime,
                        input.AllowedToTime
                    )
                )
                .GroupBy(c => (c.RoomId, c.Local.Hour))
                .Select(g => g.First())
                .OrderBy(c => c.StartTime)
                .Take(BundleLimits.RetailSampleShowtimes)
                .Select(c => c.ShowtimeId),
        ];
    }
}
