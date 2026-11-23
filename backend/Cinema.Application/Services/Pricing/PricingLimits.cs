namespace Cinema.Application.Services.Pricing;

public static class PricingLimits
{
    public const decimal MinTicketTypeMultiplier = 0.01m;
    public const decimal MaxTicketTypeMultiplier = 5m;
    public const decimal MinRuleMultiplier = 0.1m;
    public const decimal MaxRuleMultiplier = 5m;
    public const decimal MaxRuleAmount = 10_000_000m;
    public const int MaxPriority = 1000;
    public const int MaxOccupancyTiers = 20;
    public const int MaxHoursBeforeStart = 720;
    public const int MaxSpecialDaySpanDays = 31;
    public const int MaxIdListSize = 200;
}
