using System.Globalization;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;

namespace Cinema.Application.Services.Pricing;

public static class PricingMapper
{
    public static string? FormatTime(TimeOnly? value) =>
        value?.ToString(DateTimePatterns.HourMinute, CultureInfo.InvariantCulture);

    public static List<int> NormalizeIds(IEnumerable<int>? ids) =>
        ids is null ? [] : [.. ids.Distinct().Order()];

    public static TicketTypeDto ToDto(TicketType t) =>
        new(
            t.TicketTypeId,
            t.Name,
            t.PriceMultiplier,
            t.RequiresCardVerification,
            [.. t.AllowedAgeRatingIds],
            FormatTime(t.AllowedFromTime),
            FormatTime(t.AllowedToTime),
            [.. t.AllowedDaysOfWeek],
            t.IsActive,
            t.CreatedAt,
            t.UpdatedAt
        );

    public static SpecialDayDto ToDto(SpecialDay d) =>
        new(d.SpecialDayId, d.Name, d.StartDate, d.EndDate, d.IsActive, d.CreatedAt, d.UpdatedAt);

    public static PricingRuleDto ToDto(PricingRule r) =>
        new(
            r.PricingRuleId,
            r.Name,
            r.RuleType.ToString(),
            ToDto(PricingRuleCondition.TryParse(r.ConditionJson) ?? new PricingRuleCondition()),
            r.AdjustmentMode.ToString(),
            r.AdjustmentValue,
            r.Priority,
            r.ValidFrom,
            r.ValidTo,
            [.. r.RoomIds],
            [.. r.ScreenTypeIds],
            [.. r.MovieIds],
            r.IsActive,
            r.CreatedAt,
            r.UpdatedAt
        );

    public static PricingRuleConditionDto ToDto(PricingRuleCondition c) =>
        new(
            FormatTime(c.StartTime),
            FormatTime(c.EndTime),
            c.DaysOfWeek is null ? null : [.. c.DaysOfWeek],
            c.SpecialDayIds is null ? null : [.. c.SpecialDayIds],
            c.Tiers
                ?.Select(t => new OccupancyTierDto(
                    t.FromPercent,
                    t.ToPercent,
                    t.MaxHoursBeforeStart,
                    t.Value
                ))
                .ToList()
        );

    public static PricingRuleCondition ToCondition(
        PricingRuleType type,
        PricingRuleConditionDto dto
    ) =>
        type switch
        {
            PricingRuleType.TimeOfDay => new PricingRuleCondition
            {
                StartTime = DateTimeUtils.ParseTimeOnly(dto.StartTime),
                EndTime = DateTimeUtils.ParseTimeOnly(dto.EndTime),
            },
            PricingRuleType.WeekDay => new PricingRuleCondition
            {
                DaysOfWeek = NormalizeIds(dto.DaysOfWeek),
            },
            PricingRuleType.SpecialDay => new PricingRuleCondition
            {
                SpecialDayIds = NormalizeIds(dto.SpecialDayIds),
            },
            _ => new PricingRuleCondition
            {
                Tiers =
                [
                    .. (dto.Tiers ?? [])
                        .OrderBy(t => t.FromPercent)
                        .Select(t => new OccupancyTier(
                            t.FromPercent,
                            t.ToPercent,
                            t.MaxHoursBeforeStart,
                            t.Value
                        )),
                ],
            },
        };
}
