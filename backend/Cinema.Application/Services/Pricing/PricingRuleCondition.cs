using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cinema.Application.Services.Pricing;

public sealed record OccupancyTier(
    decimal FromPercent,
    decimal? ToPercent,
    int? MaxHoursBeforeStart,
    decimal Value
);

public sealed record PricingRuleCondition
{
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public List<int>? DaysOfWeek { get; init; }
    public List<int>? SpecialDayIds { get; init; }
    public List<OccupancyTier>? Tiers { get; init; }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ToJson() => JsonSerializer.Serialize(this, Options);

    public static PricingRuleCondition? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PricingRuleCondition>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
