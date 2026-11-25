
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
    public const string FinanceReport = "finance";
    public const string SalesStatsRebuild = "sales-stats";
    public const string Reconciliation = "reconciliation";
    public const string ReconciliationStripe = "reconciliation-stripe";
    public const string GiftCardBulk = "gift-card-bulk";

    private static readonly HashSet<string> SensitiveModules =
    [
        Users,
        Orders,
        Payments,
        PromotionRedemptions,
        Reconciliation,
        ReconciliationStripe,
    ];

    private static readonly HashSet<string> PersonalDataModules =
    [
        Users,
        Orders,
        PromotionRedemptions,
        GiftCardBulk,
    ];

    public static bool ContainsPersonalData(string module) => PersonalDataModules.Contains(module);

    public static bool IsSensitive(string module) => SensitiveModules.Contains(module);
}
