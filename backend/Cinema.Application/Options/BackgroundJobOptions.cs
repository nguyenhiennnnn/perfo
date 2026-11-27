namespace Cinema.Application.Options;

public class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public string ReleaseExpiredSeatsCron { get; set; } = "* * * * *";
    public string ShowtimeLifecycleCron { get; set; } = "* * * * *";
    public string ExpireLoyaltyPointsCron { get; set; } = "5 * * * *";
    public string LoyaltyExpiryReminderCron { get; set; } = "0 9 * * *";
    public string MembershipExpiryReminderCron { get; set; } = "0 9 * * *";
    public string MembershipExpiryCron { get; set; } = "*/15 * * * *";
    public string MembershipFreeTicketGrantCron { get; set; } = "*/5 * * * *";
    public string MembershipRevenueRecognitionCron { get; set; } = "*/10 * * * *";
    public string CleanupImportExportFilesCron { get; set; } = "*/30 * * * *";
    public string CleanupImportSessionsCron { get; set; } = "*/5 * * * *";
    public string ProcessOutboxCron { get; set; } = "* * * * *";
    public string LowStockAlertCron { get; set; } = "*/15 * * * *";
    public string SalesStatsRefreshCron { get; set; } = "*/5 * * * *";
    public string UnclosedShiftAlertCron { get; set; } = "*/15 * * * *";
    public string GiftCardDispatchCron { get; set; } = "* * * * *";
    public string GiftCardExpiryCron { get; set; } = "*/15 * * * *";
    public string ShowtimeReminderCron { get; set; } = "* * * * *";
    public string MovieAffinityRefreshCron { get; set; } = "20 * * * *";
    public string CustomerSegmentRefreshCron { get; set; } = "40 */6 * * *";
    public string ReviewInviteCron { get; set; } = "*/5 * * * *";
    public string MovieIntroEmailCron { get; set; } = "0 10 * * *";
    public string RoomQualityAlertCron { get; set; } = "*/30 * * * *";
    public string WaitlistCloseCron { get; set; } = "* * * * *";
    public string WaitlistPriorityExpiryCron { get; set; } = "* * * * *";
    public string RoomRentalQuoteExpiryCron { get; set; } = "*/5 * * * *";
    public string RoomRentalRevenueRecognitionCron { get; set; } = "*/10 * * * *";
    public string RoomRentalBalanceOverdueCron { get; set; } = "*/10 * * * *";
    public int OutboxBatchSize { get; set; } = 50;
    public int OutboxMaxAttempts { get; set; } = 5;
    public int[] OutboxRetryDelaysSeconds { get; set; } = [30, 120, 600, 1800];
    public int OutboxLeaseSeconds { get; set; } = 300;
    public int OutboxRetentionDays { get; set; } = 14;
    public int RetryAttempts { get; set; } = 3;
    public int[] RetryDelaysSeconds { get; set; } = [30, 120, 600];
}
