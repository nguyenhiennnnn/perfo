namespace Cinema.Shared.Constants;

public class RecurringJobIds
{
    public const string ReleaseExpiredSeats = "release-expired-seats-and-bookings";
    public const string UpdateShowtimeLifecycle = "update-showtime-lifecycle";
    public const string ExpireLoyaltyPoints = "expire-loyalty-points";
    public const string LoyaltyExpiryReminder = "loyalty-expiry-reminder";
    public const string CleanupImportExportFiles = "cleanup-import-export-files";
    public const string CleanupImportSessions = "cleanup-import-sessions";
    public const string ProcessOutbox = "process-outbox";
    public const string LowStockAlert = "low-stock-alert";
    public const string RefreshSalesStats = "refresh-sales-stats";
    public const string UnclosedShiftAlert = "unclosed-shift-alert";
    public const string GiftCardDispatch = "gift-card-dispatch";
    public const string GiftCardExpiry = "gift-card-expiry";
    public const string ShowtimeReminder = "showtime-reminder";
    public const string RefreshMovieAffinity = "refresh-movie-affinity";
    public const string ReviewInvite = "review-invite";
    public const string MovieIntroEmail = "movie-intro-email";
    public const string RoomQualityAlert = "room-quality-alert";
    public const string WaitlistClose = "waitlist-close";
    public const string WaitlistPriorityExpiry = "waitlist-priority-expiry";
}
