namespace Cinema.Shared.Constants;

public class RecurringJobIds
{
    public const string ReleaseExpiredSeats = "release-expired-seats-and-bookings";
    public const string UpdateShowtimeLifecycle = "update-showtime-lifecycle";
    public const string ExpireLoyaltyPoints = "expire-loyalty-points";
    public const string CleanupImportExportFiles = "cleanup-import-export-files";
    public const string CleanupImportSessions = "cleanup-import-sessions";
    public const string ProcessOutbox = "process-outbox";
    public const string LowStockAlert = "low-stock-alert";
    public const string RefreshSalesStats = "refresh-sales-stats";
}
