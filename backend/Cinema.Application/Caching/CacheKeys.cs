namespace Cinema.Application.Caching;

public static class CacheKeys
{
    public static readonly CacheKey<CinemaSettingsSnapshot> CinemaSettings =
        new("cinema-settings", CacheGroups.CinemaSettings);

    public static readonly CacheKey<IReadOnlyList<MembershipTierSnapshot>> MembershipTiers =
        new("membership-tiers", CacheGroups.MembershipTiers);
}
