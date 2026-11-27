using Cinema.Application.Utils;

namespace Cinema.Application.Services.Retention;

public static class RetentionRules
{
    public const int MaxMonthOffset = 3;
    public const int DefaultReportMonths = 12;
    public const int MaxReportMonths = 36;

    public static DateOnly MonthOf(DateTime utc)
    {
        var local = BusinessTime.LocalDate(utc);
        return new DateOnly(local.Year, local.Month, 1);
    }

    public static DateTime StartUtc(DateOnly month) => BusinessTime.StartOfDayUtc(month);

    public static bool HasStarted(DateOnly month, DateTime utcNow) => StartUtc(month) <= utcNow;

    public static bool IsComplete(DateOnly month, DateTime utcNow) =>
        StartUtc(month.AddMonths(1)) <= utcNow;
}
