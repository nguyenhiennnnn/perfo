namespace Cinema.Application.Caching;

public static class CacheKeys
{
    public static readonly CacheKey<CinemaSettingsSnapshot> CinemaSettings =
        new("cinema-settings", CacheGroups.CinemaSettings);

    public static readonly CacheKey<IReadOnlyList<MembershipTierSnapshot>> MembershipTiers =
        new("membership-tiers", CacheGroups.MembershipTiers);

    public static readonly CacheKey<IReadOnlyList<TicketTypeSnapshot>> TicketTypes =
        new("ticket-types", CacheGroups.TicketTypes);

    public static readonly CacheKey<IReadOnlyList<PricingRuleSnapshot>> PricingRules =
        new("pricing-rules", CacheGroups.PricingRules);

    public static readonly CacheKey<IReadOnlyList<SpecialDaySnapshot>> HolidayCalendar =
        new("holiday-calendar", CacheGroups.HolidayCalendar);
}
