using Cinema.Application.Caching;

namespace Cinema.Application.Services.Pricing;

public enum ShowtimePricingMode
{
    Standard,
    Private,
    Event,
}

public sealed record PricingScope(
    ShowtimePricingMode Mode,
    IReadOnlyList<PricingRuleSnapshot> EventRules
)
{
    public static PricingScope Standard { get; } = new(ShowtimePricingMode.Standard, []);
}

public sealed record AppliedRule(PricingRuleSnapshot Rule, decimal Value, string Reason);

public sealed record PricingParameters(
    decimal RoundingStep,
    decimal MaxIncreasePercent,
    decimal MaxDecreasePercent,
    decimal MinTicketPrice
)
{
    public static PricingParameters From(CinemaSettingsSnapshot settings) =>
        new(
            settings.TicketPriceRoundingStep,
            settings.DynamicPricingMaxIncreasePercent,
            settings.DynamicPricingMaxDecreasePercent,
            settings.MinTicketPrice
        );
}

public sealed record OccupancySnapshot(
    int TotalSeats,
    int BookedSeats,
    int HeldSeats,
    decimal? SalesPerHour
)
{
    public decimal Percent =>
        TotalSeats <= 0 ? 0m : (BookedSeats + HeldSeats) * 100m / TotalSeats;
}
