namespace Cinema.Application.Services.Promotions;

public static class PromotionTimeSlots
{
    public const string Morning = "Morning";
    public const string Afternoon = "Afternoon";
    public const string Evening = "Evening";
    public const string Night = "Night";

    public static readonly IReadOnlyList<string> All = [Morning, Afternoon, Evening, Night];

    public static string FromHour(int hour) =>
        hour switch
        {
            >= 6 and < 12 => Morning,
            >= 12 and < 17 => Afternoon,
            >= 17 and < 22 => Evening,
            _ => Night,
        };
}
