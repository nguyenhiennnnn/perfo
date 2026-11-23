using Cinema.Application.Services.Pricing;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Caching;

public sealed record TicketTypeSnapshot(
    int TicketTypeId,
    string Name,
    decimal PriceMultiplier,
    bool RequiresCardVerification,
    IReadOnlyList<int> AllowedAgeRatingIds,
    TimeOnly? AllowedFromTime,
    TimeOnly? AllowedToTime,
    IReadOnlyList<int> AllowedDaysOfWeek
)
{
    public static TicketTypeSnapshot From(TicketType t) =>
        new(
            t.TicketTypeId,
            t.Name,
            t.PriceMultiplier,
            t.RequiresCardVerification,
            [.. t.AllowedAgeRatingIds],
            t.AllowedFromTime,
            t.AllowedToTime,
            [.. t.AllowedDaysOfWeek]
        );
}

public sealed record PricingRuleSnapshot(
    int PricingRuleId,
    string Name,
    PricingRuleType RuleType,
    PricingRuleCondition Condition,
    PricingAdjustmentMode AdjustmentMode,
    decimal? AdjustmentValue,
    int Priority,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    IReadOnlyList<int> RoomIds,
    IReadOnlyList<int> ScreenTypeIds,
    IReadOnlyList<int> MovieIds
)
{
    public static PricingRuleSnapshot? From(PricingRule r)
    {
        var condition = PricingRuleCondition.TryParse(r.ConditionJson);
        return condition is null
            ? null
            : new PricingRuleSnapshot(
                r.PricingRuleId,
                r.Name,
                r.RuleType,
                condition,
                r.AdjustmentMode,
                r.AdjustmentValue,
                r.Priority,
                r.ValidFrom,
                r.ValidTo,
                [.. r.RoomIds],
                [.. r.ScreenTypeIds],
                [.. r.MovieIds]
            );
    }
}

public sealed record SpecialDaySnapshot(
    int SpecialDayId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate
)
{
    public static SpecialDaySnapshot From(SpecialDay d) =>
        new(d.SpecialDayId, d.Name, d.StartDate, d.EndDate);
}
