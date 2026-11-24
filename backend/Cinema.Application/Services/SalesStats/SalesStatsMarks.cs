namespace Cinema.Application.Services.SalesStats;

public static class SalesStatsMarks
{
    public const int DayBeforeMinutes = 1440;
    public const int ThreeHoursBeforeMinutes = 180;

    public static IReadOnlyList<int> For(int customMarkMinutes)
    {
        var marks = new List<int> { DayBeforeMinutes, ThreeHoursBeforeMinutes };
        if (customMarkMinutes > 0 && !marks.Contains(customMarkMinutes))
            marks.Add(customMarkMinutes);

        return [.. marks.OrderByDescending(m => m)];
    }

    public static int LookaheadMinutes(int customMarkMinutes) =>
        Math.Max(DayBeforeMinutes, customMarkMinutes);
}
