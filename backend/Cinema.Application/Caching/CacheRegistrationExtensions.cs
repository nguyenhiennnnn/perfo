using Cinema.Application.Interfaces;
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
            );
}
