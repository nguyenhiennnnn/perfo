using System.Globalization;
using Cinema.Application.Caching;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Pricing;

public sealed record RuleMatchContext(
    int RoomId,
    int? ScreenTypeId,
    int MovieId,
    DateOnly BusinessDate,
    TimeOnly LocalTime,
    decimal HoursBeforeStart,
    decimal OccupancyPercent,
    IReadOnlyList<SpecialDaySnapshot> SpecialDays
);

public static class PricingRuleMatcher
{
    public static IReadOnlyList<AppliedRule> Match(
        IEnumerable<PricingRuleSnapshot> rules,
        RuleMatchContext context
    )
    {
        var applied = new Dictionary<PricingRuleType, AppliedRule>();
        foreach (
            var rule in rules.OrderByDescending(r => r.Priority).ThenBy(r => r.PricingRuleId)
        )
        {
            if (applied.ContainsKey(rule.RuleType) || !InScope(rule, context))
                continue;

            if (Evaluate(rule, context) is { } match)
                applied[rule.RuleType] = match;
        }

        return [.. applied.Values.OrderBy(a => a.Rule.RuleType)];
    }

    private static bool InScope(PricingRuleSnapshot rule, RuleMatchContext context) =>
        (rule.ValidFrom is null || context.BusinessDate >= rule.ValidFrom)
        && (rule.ValidTo is null || context.BusinessDate <= rule.ValidTo)
        && (rule.RoomIds.Count == 0 || rule.RoomIds.Contains(context.RoomId))
        && (
            rule.ScreenTypeIds.Count == 0
            || (
                context.ScreenTypeId is { } screenTypeId
                && rule.ScreenTypeIds.Contains(screenTypeId)
            )
        )
        && (rule.MovieIds.Count == 0 || rule.MovieIds.Contains(context.MovieId));

    private static AppliedRule? Evaluate(PricingRuleSnapshot rule, RuleMatchContext context) =>
        rule.RuleType switch
        {
            PricingRuleType.TimeOfDay => EvaluateTimeOfDay(rule, context),
            PricingRuleType.WeekDay => EvaluateWeekDay(rule, context),
            PricingRuleType.SpecialDay => EvaluateSpecialDay(rule, context),
            _ => EvaluateOccupancy(rule, context),
        };

    private static AppliedRule? EvaluateTimeOfDay(
        PricingRuleSnapshot rule,
        RuleMatchContext context
    )
    {
        if (
            rule.AdjustmentValue is not { } value
            || rule.Condition.StartTime is not { } start
            || rule.Condition.EndTime is not { } end
            || context.LocalTime < start
            || context.LocalTime >= end
        )
            return null;

        return new AppliedRule(
            rule,
            value,
            string.Format(
                Messages.Pricing_Reason_TimeOfDay,
                FormatTime(context.LocalTime),
                FormatTime(start),
                FormatTime(end)
            )
        );
    }

    private static AppliedRule? EvaluateWeekDay(
        PricingRuleSnapshot rule,
        RuleMatchContext context
    )
    {
        if (
            rule.AdjustmentValue is not { } value
            || rule.Condition.DaysOfWeek is not { Count: > 0 } days
            || !days.Contains((int)context.BusinessDate.DayOfWeek)
        )
            return null;

        return new AppliedRule(
            rule,
            value,
            string.Format(
                Messages.Pricing_Reason_WeekDay,
                context.BusinessDate.ToString(
                    DateTimePatterns.Date,
                    CultureInfo.InvariantCulture
                )
            )
        );
    }

    private static AppliedRule? EvaluateSpecialDay(
        PricingRuleSnapshot rule,
        RuleMatchContext context
    )
    {
        if (rule.AdjustmentValue is not { } value)
            return null;

        var ids = rule.Condition.SpecialDayIds;
        var specialDay = context.SpecialDays.FirstOrDefault(d =>
            context.BusinessDate >= d.StartDate
            && context.BusinessDate <= d.EndDate
            && (ids is not { Count: > 0 } || ids.Contains(d.SpecialDayId))
        );
        if (specialDay is null)
            return null;

        return new AppliedRule(
            rule,
            value,
            string.Format(
                Messages.Pricing_Reason_SpecialDay,
                context.BusinessDate.ToString(
                    DateTimePatterns.Date,
                    CultureInfo.InvariantCulture
                ),
                specialDay.Name
            )
        );
    }

    private static AppliedRule? EvaluateOccupancy(
        PricingRuleSnapshot rule,
        RuleMatchContext context
    )
    {
        var tier = rule.Condition.Tiers?.FirstOrDefault(t => TierMatches(t, context));
        if (tier is null)
            return null;

        var percent = FormatNumber(context.OccupancyPercent);
        var reason =
            tier.ToPercent is { } to
                ? string.Format(
                    Messages.Pricing_Reason_OccupancyTier,
                    percent,
                    FormatNumber(tier.FromPercent),
                    FormatNumber(to)
                )
                : string.Format(
                    Messages.Pricing_Reason_OccupancyTierOpen,
                    percent,
                    FormatNumber(tier.FromPercent)
                );
        if (tier.MaxHoursBeforeStart is { } maxHours)
            reason += string.Format(
                Messages.Pricing_Reason_OccupancyHours,
                FormatNumber(context.HoursBeforeStart),
                maxHours
            );

        return new AppliedRule(rule, tier.Value, reason);
    }

    private static bool TierMatches(OccupancyTier tier, RuleMatchContext context)
    {
        var percent = context.OccupancyPercent;
        var withinPercent =
            percent >= tier.FromPercent
            && (
                tier.ToPercent is null
                || percent < tier.ToPercent
                || (tier.ToPercent == 100m && percent >= 100m)
            );

        return withinPercent
            && (
                tier.MaxHoursBeforeStart is null
                || context.HoursBeforeStart <= tier.MaxHoursBeforeStart
            );
    }

    private static string FormatTime(TimeOnly value) =>
        value.ToString(DateTimePatterns.HourMinute, CultureInfo.InvariantCulture);

    private static string FormatNumber(decimal value) =>
        value.ToString("0.#", CultureInfo.InvariantCulture);
}
