namespace Cinema.Application.Services.Promotions;

public static class PromotionLimits
{
    public const int MinCodeLength = 3;
    public const decimal MaxAmount = 1_000_000_000m;
    public const int MaxUsageLimit = 10_000_000;
    public const int MaxPriority = 1000;
}
