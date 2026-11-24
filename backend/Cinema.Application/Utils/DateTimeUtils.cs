using System.Globalization;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;

namespace Cinema.Application.Utils;

public static class DateTimeUtils
{
    public static TimeOnly? ParseTimeOnly(string? value, string format = DateTimePatterns.HourMinute)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return TimeOnly.ParseExact(value.Trim(), format, CultureInfo.InvariantCulture);
    }

    public static string FormatDuration(TimeSpan span) =>
        span.TotalMinutes >= 1
            ? string.Format(Messages.Common_DurationMinutes, (int)Math.Ceiling(span.TotalMinutes))
            : string.Format(Messages.Common_DurationSeconds, (int)Math.Ceiling(span.TotalSeconds));

    public static string GetPeriodKey(DateTime createdAt, string groupBy) =>
        GetPeriodKey(DateOnly.FromDateTime(BusinessTime.ToLocal(createdAt)), groupBy);

    public static string GetPeriodKey(DateOnly date, string groupBy)
    {
        var local = date.ToDateTime(TimeOnly.MinValue);
        return groupBy switch
        {
            "day" => local.ToString(DateTimePatterns.Date, CultureInfo.InvariantCulture),
            "week" => $"{ISOWeek.GetYear(local)}-W{ISOWeek.GetWeekOfYear(local):D2}",
            "month" => local.ToString("MM/yyyy", CultureInfo.InvariantCulture),
            _ => local.ToString(DateTimePatterns.Date, CultureInfo.InvariantCulture),
        };
    }

    public static DateTime GetStartOfDay(DateOnly date) => BusinessTime.StartOfDayUtc(date);

    public static DateTime GetStartOfNextDay(DateOnly date) => BusinessTime.StartOfNextDayUtc(date);
}
