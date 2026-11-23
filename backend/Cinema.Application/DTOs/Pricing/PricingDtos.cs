namespace Cinema.Application.DTOs.Pricing;

public record TicketTypeDto(
    int TicketTypeId,
    string Name,
    decimal PriceMultiplier,
    bool RequiresCardVerification,
    List<int> AllowedAgeRatingIds,
    string? AllowedFromTime,
    string? AllowedToTime,
    List<int> AllowedDaysOfWeek,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertTicketTypeRequest(
    string Name,
    decimal PriceMultiplier,
    bool RequiresCardVerification,
    List<int>? AllowedAgeRatingIds,
    string? AllowedFromTime,
    string? AllowedToTime,
    List<int>? AllowedDaysOfWeek,
    bool IsActive = true
);

public record TicketTypeListFilter(string? Search, bool? IsActive);

public record OccupancyTierDto(
    decimal FromPercent,
    decimal? ToPercent,
    int? MaxHoursBeforeStart,
    decimal Value
);

public record PricingRuleConditionDto(
    string? StartTime,
    string? EndTime,
    List<int>? DaysOfWeek,
    List<int>? SpecialDayIds,
    List<OccupancyTierDto>? Tiers
);

public record PricingRuleDto(
    int PricingRuleId,
    string Name,
    string RuleType,
    PricingRuleConditionDto Condition,
    string AdjustmentMode,
    decimal? AdjustmentValue,
    int Priority,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    List<int> RoomIds,
    List<int> ScreenTypeIds,
    List<int> MovieIds,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertPricingRuleRequest(
    string Name,
    string RuleType,
    PricingRuleConditionDto? Condition,
    string AdjustmentMode,
    decimal? AdjustmentValue,
    int Priority,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    List<int>? RoomIds,
    List<int>? ScreenTypeIds,
    List<int>? MovieIds,
    bool IsActive = true
);

public record PricingRuleListFilter(string? Search, string? RuleType, bool? IsActive);

public record SpecialDayDto(
    int SpecialDayId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertSpecialDayRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive = true
);

public record SpecialDayListFilter(string? Search, bool? IsActive, int? Year);

public record DeletePricingEntityResponse(int Id, bool Deleted);
