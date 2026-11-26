using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cinema.Application.Caching;

public static class CacheRegistrationExtensions
{
    public static IServiceCollection AddCachedData<T>(
        this IServiceCollection services,
        CacheKey<T> key,
        Func<IAppDbContext, CancellationToken, Task<T>> loader,
        TimeSpan? ttl = null
    )
        where T : class
    {
        services.AddSingleton(
            new CacheDefinition(
                key.Name,
                key.Group,
                async (provider, cancellationToken) =>
                    await loader(provider.GetRequiredService<IAppDbContext>(), cancellationToken),
                ttl
            )
        );
        return services;
    }

    public static IServiceCollection AddCinemaCaches(this IServiceCollection services) =>
        services
            .AddCachedData(
                CacheKeys.CinemaSettings,
                async (db, ct) =>
                    CinemaSettingsSnapshot.From(await db.LoadCinemaSettingsOrDefaultAsync(ct))
            )
            .AddCachedData<IReadOnlyList<MembershipTierSnapshot>>(
                CacheKeys.MembershipTiers,
                async (db, ct) =>
                    await db
                        .MembershipTiers.AsNoTracking()
                        .OrderBy(t => t.MinPoints)
                        .Select(t => new MembershipTierSnapshot(
                            t.TierId,
                            t.TierName,
                            t.MinPoints,
                            t.DiscountPercent
                        ))
                        .ToListAsync(ct)
            )
            .AddCachedData<IReadOnlyList<TicketTypeSnapshot>>(
                CacheKeys.TicketTypes,
                async (db, ct) =>
                    (
                        await db
                            .TicketTypes.AsNoTracking()
                            .Where(t => t.IsActive)
                            .OrderBy(t => t.PriceMultiplier)
                            .ThenBy(t => t.TicketTypeId)
                            .ToListAsync(ct)
                    )
                        .Select(TicketTypeSnapshot.From)
                        .ToList()
            )
            .AddCachedData<IReadOnlyList<PricingRuleSnapshot>>(
                CacheKeys.PricingRules,
                async (db, ct) =>
                    (
                        await db
                            .PricingRules.AsNoTracking()
                            .Where(r => r.IsActive)
                            .OrderByDescending(r => r.Priority)
                            .ThenBy(r => r.PricingRuleId)
                            .ToListAsync(ct)
                    )
                        .Select(PricingRuleSnapshot.From)
                        .OfType<PricingRuleSnapshot>()
                        .ToList()
            )
            .AddCachedData<IReadOnlyList<PromotionSnapshot>>(
                CacheKeys.Promotions,
                async (db, ct) =>
                    (
                        await db
                            .Promotions.AsNoTracking()
                            .Where(p => p.IsActive && p.Type != PromotionType.PersonalCode)
                            .OrderByDescending(p => p.Priority)
                            .ThenBy(p => p.PromotionId)
                            .ToListAsync(ct)
                    )
                        .Select(PromotionSnapshot.From)
                        .ToList()
            )
            .AddCachedData<IReadOnlyList<BundleSnapshot>>(
                CacheKeys.Bundles,
                async (db, ct) =>
                    (
                        await db
                            .Bundles.AsNoTracking()
                            .Include(b => b.Items)
                            .Where(b => b.IsActive)
                            .OrderBy(b => b.Price)
                            .ThenBy(b => b.BundleId)
                            .ToListAsync(ct)
                    )
                        .Select(BundleSnapshot.From)
                        .ToList()
            )
            .AddCachedData<IReadOnlyList<SpecialDaySnapshot>>(
                CacheKeys.HolidayCalendar,
                async (db, ct) =>
                    (
                        await db
                            .SpecialDays.AsNoTracking()
                            .Where(d => d.IsActive)
                            .OrderBy(d => d.StartDate)
                            .ThenBy(d => d.SpecialDayId)
                            .ToListAsync(ct)
                    )
                        .Select(SpecialDaySnapshot.From)
                        .ToList()
            )
            .AddCachedData<IReadOnlyList<RoomRentalRateSnapshot>>(
                CacheKeys.RoomRentalRates,
                async (db, ct) =>
                    (
                        await db
                            .RoomRentalRates.AsNoTracking()
                            .Where(r => r.IsActive)
                            .OrderBy(r => r.RoomId)
                            .ThenBy(r => r.StartTime)
                            .ThenBy(r => r.MinDurationMinutes)
                            .ThenBy(r => r.RoomRentalRateId)
                            .ToListAsync(ct)
                    )
                        .Select(RoomRentalRateSnapshot.From)
                        .ToList()
            );
}
