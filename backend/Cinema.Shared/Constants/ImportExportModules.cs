
namespace Cinema.Shared.Constants;

public class ImportExportModules
{
    public const string Movies = "movies";
    public const string Showtimes = "showtimes";
    public const string Orders = "bookings";
    public const string Payments = "payments";
    public const string PromotionRedemptions = "promotion-redemptions";
    public const string Users = "users";
    public const string SeatLayout = "seat-layout";
    public const string RevenueReport = "revenue";
    public const string TopMoviesReport = "top-movies";
    public const string EmployeePerformanceReport = "employee-performance";

    private static readonly HashSet<string> SensitiveModules =
    [
        Users,
        Orders,
        Payments,
        PromotionRedemptions,
    ];

    private static readonly HashSet<string> PersonalDataModules =
    [
        Users,
        Orders,
        PromotionRedemptions,
    ];

    public static bool ContainsPersonalData(string module) => PersonalDataModules.Contains(module);

    public static bool IsSensitive(string module) => SensitiveModules.Contains(module);
}
