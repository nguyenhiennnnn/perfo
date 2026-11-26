using Cinema.Application.Caching;
using Cinema.Application.DTOs.Bundles;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces.Bundles;
using Cinema.Application.Interfaces.Combos;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bundles;

public class BundleAvailabilityService(
    IAppDbContext db,
    IAppCache cache,
    IPricingService pricingService,
    IComboAvailabilityService comboAvailability
) : IBundleAvailabilityService
{
    public async Task<IReadOnlyList<ApplicableBundleDto>> GetForShowtimeAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var showtime =
            await db
                .Showtimes.AsNoTracking()
                .Where(s => s.ShowtimeId == showtimeId)
                .Select(s => new
                {
                    s.MovieId,
                    s.StartTime,
                    s.Kind,
                    s.Status,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);

        var now = DateTime.UtcNow;
        if (
            showtime.Kind != ShowtimeKind.Regular
            || showtime.Status != ShowtimeStatus.Scheduled
            || showtime.StartTime <= now
        )
            return [];

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var local = BusinessTime.ToLocal(showtime.StartTime);
        var businessDate = PricingCalendar.BusinessDate(
            local,
            settings.OpeningTime,
            settings.ClosingTime
        );
        var localTime = TimeOnly.FromDateTime(local);
        var dayOfWeek = (int)businessDate.DayOfWeek;

        var candidates = (await cache.GetAsync(CacheKeys.Bundles, cancellationToken))
            .Where(b =>
                (b.ValidFrom is not { } validFrom || validFrom <= now)
                && (b.ValidTo is not { } validTo || validTo > now)
                && (b.ShowtimeIds.Count == 0 || b.ShowtimeIds.Contains(showtimeId))
                && (b.MovieIds.Count == 0 || b.MovieIds.Contains(showtime.MovieId))
                && (b.DaysOfWeek.Count == 0 || b.DaysOfWeek.Contains(dayOfWeek))
                && BundleRetailCalculator.InTimeWindow(
                    localTime,
                    b.AllowedFromTime,
                    b.AllowedToTime
                )
            )
            .ToList();
        if (candidates.Count == 0)
            return [];

        var table = await pricingService.GetPriceTableAsync(showtimeId, now, cancellationToken);
        var usage = await LoadUsageAsync(candidates, businessDate, cancellationToken);
        var combos = await LoadCombosAsync(candidates, cancellationToken);
        var ticketNames = table.TicketTypes.ToDictionary(t => t.TicketTypeId, t => t.Name);

        var result = new List<ApplicableBundleDto>();
        foreach (var bundle in candidates)
        {
            int? remaining = null;
            if (bundle.DailyLimit is { } limit)
            {
                remaining = limit - usage.GetValueOrDefault(bundle.BundleId);
                if (remaining <= 0)
                    continue;
            }

            if (
                await TryBuildAsync(bundle, table, ticketNames, combos, remaining, cancellationToken)
                is { } dto
            )
                result.Add(dto);
        }

        return
        [
            .. result
                .OrderByDescending(b => b.SavingsAmount)
                .ThenBy(b => b.Price)
                .ThenBy(b => b.BundleId),
        ];
    }

    private async Task<ApplicableBundleDto?> TryBuildAsync(
        BundleSnapshot bundle,
        ShowtimePriceTableDto table,
        IReadOnlyDictionary<int, string> ticketNames,
        IReadOnlyDictionary<int, (string Name, decimal Price)> combos,
        int? remaining,
        CancellationToken cancellationToken
    )
    {
        var ticketItems = bundle.Items.Where(i => i.TicketTypeId is not null).ToList();
        var comboItems = bundle.Items.Where(i => i.ComboId is not null).ToList();
        if (ticketItems.Count == 0 || comboItems.Any(i => !combos.ContainsKey(i.ComboId!.Value)))
            return null;
        if (ticketItems.Any(i => !ticketNames.ContainsKey(i.TicketTypeId!.Value)))
            return null;
        if (BundleRetailCalculator.TicketTotal(table, ticketItems, bundle.SeatTypeIds) is not { } ticketTotal)
            return null;

        var retail = ticketTotal + comboItems.Sum(i => combos[i.ComboId!.Value].Price * i.Quantity);
        var savings = retail - bundle.Price;
        if (savings <= 0m)
            return null;

        try
        {
            await comboAvailability.EnsureAvailableAsync(
                [.. comboItems.Select(i => (i.ComboId!.Value, i.Quantity))],
                cancellationToken
            );
        }
        catch (ConflictAppException)
        {
            return null;
        }

        var items = bundle
            .Items.Select(i =>
                i.TicketTypeId is { } ticketTypeId
                    ? new ApplicableBundleItemDto(
                        ticketTypeId,
                        null,
                        ticketNames[ticketTypeId],
                        i.Quantity,
                        BundleRetailCalculator.TicketTotal(table, [i], bundle.SeatTypeIds)!.Value
                            / i.Quantity
                    )
                    : new ApplicableBundleItemDto(
                        null,
                        i.ComboId,
                        combos[i.ComboId!.Value].Name,
                        i.Quantity,
                        combos[i.ComboId.Value].Price
                    )
            )
            .ToList();

        return new ApplicableBundleDto(
            bundle.BundleId,
            bundle.Name,
            bundle.Description,
            bundle.Price,
            ticketItems.Sum(i => i.Quantity),
            items,
            retail,
            savings,
            decimal.Round(savings / retail * 100m, 1),
            bundle.ExcludesPromotions,
            remaining
        );
    }

    private async Task<Dictionary<int, int>> LoadUsageAsync(
        IReadOnlyList<BundleSnapshot> candidates,
        DateOnly businessDate,
        CancellationToken cancellationToken
    )
    {
        var limited = candidates
            .Where(b => b.DailyLimit is not null)
            .Select(b => b.BundleId)
            .ToList();
        if (limited.Count == 0)
            return [];

        return await db
            .BundleDailyUsages.AsNoTracking()
            .Where(u => limited.Contains(u.BundleId) && u.BusinessDate == businessDate)
            .ToDictionaryAsync(u => u.BundleId, u => u.UsedCount, cancellationToken);
    }

    private async Task<Dictionary<int, (string Name, decimal Price)>> LoadCombosAsync(
        IReadOnlyList<BundleSnapshot> candidates,
        CancellationToken cancellationToken
    )
    {
        var comboIds = candidates
            .SelectMany(b => b.Items)
            .Where(i => i.ComboId is not null)
            .Select(i => i.ComboId!.Value)
            .Distinct()
            .ToList();
        if (comboIds.Count == 0)
            return [];

        var rows = await db
            .Combos.AsNoTracking()
            .Where(c => comboIds.Contains(c.ComboId) && c.IsActive)
            .Select(c => new
            {
                c.ComboId,
                c.Name,
                c.Price,
            })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(c => c.ComboId, c => (c.Name, c.Price));
    }
}
