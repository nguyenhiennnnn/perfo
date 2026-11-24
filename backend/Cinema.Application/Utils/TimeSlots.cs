namespace Cinema.Application.Utils;

public static class TimeSlots
{
    public const string Morning = "Morning";
    public const string Afternoon = "Afternoon";
    public const string Evening = "Evening";
    public const string Late = "Late";

    public const int AfternoonStartHour = 12;
    public const int EveningStartHour = 18;
    public const int LateStartHour = 22;

    public static IReadOnlyList<string> All { get; } = [Morning, Afternoon, Evening, Late];

    public static string FromLocalHour(int hour) =>
        hour switch
        {
            < AfternoonStartHour => Morning,
            < EveningStartHour => Afternoon,
            < LateStartHour => Evening,
            _ => Late,
        };

    public static string Of(DateTime startTimeUtc) =>
        FromLocalHour(BusinessTime.ToLocal(startTimeUtc).Hour);
}
