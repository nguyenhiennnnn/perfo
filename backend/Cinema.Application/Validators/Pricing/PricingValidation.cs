using System.Globalization;
using Cinema.Application.Services.Pricing;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;

namespace Cinema.Application.Validators.Pricing;

public static class PricingValidation
{
    public static bool IsValidTime(string? value) =>
        value is not null
        && TimeOnly.TryParseExact(
            value,
            DateTimePatterns.HourMinute,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _
        );

    public static TimeOnly ParseTime(string value) =>
        TimeOnly.ParseExact(value, DateTimePatterns.HourMinute, CultureInfo.InvariantCulture);

    public static bool IsValidIdList(IReadOnlyCollection<int>? ids) =>
        ids is null
        || (
            ids.Count <= PricingLimits.MaxIdListSize
            && ids.All(id => id > 0)
            && ids.Distinct().Count() == ids.Count
        );

    public static bool IsValidDaysOfWeek(IReadOnlyCollection<int>? days) =>
        days is null
        || (days.All(d => d is >= 0 and <= 6) && days.Distinct().Count() == days.Count);

    public static bool TryParseEnum<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value)
            && !int.TryParse(value, out _)
            && Enum.TryParse(value, true, out result);
    }

    public static bool IsValidAdjustmentValue(PricingAdjustmentMode mode, decimal value) =>
        mode == PricingAdjustmentMode.Multiplier
            ? value is >= PricingLimits.MinRuleMultiplier and <= PricingLimits.MaxRuleMultiplier
                && value != 1m
                && decimal.Round(value, 4) == value
            : value != 0m
                && Math.Abs(value) <= PricingLimits.MaxRuleAmount
                && value == decimal.Truncate(value);
}
