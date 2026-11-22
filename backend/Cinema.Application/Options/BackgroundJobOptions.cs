namespace Cinema.Application.Options;

public class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public string ReleaseExpiredSeatsCron { get; set; } = "* * * * *";
    public string ShowtimeLifecycleCron { get; set; } = "* * * * *";
    public string ExpireLoyaltyPointsCron { get; set; } = "5 * * * *";
    public string CleanupImportExportFilesCron { get; set; } = "*/30 * * * *";
    public string CleanupImportSessionsCron { get; set; } = "*/5 * * * *";
    public string ProcessOutboxCron { get; set; } = "* * * * *";
    public int OutboxBatchSize { get; set; } = 50;
    public int OutboxMaxAttempts { get; set; } = 5;
    public int[] OutboxRetryDelaysSeconds { get; set; } = [30, 120, 600, 1800];
    public int OutboxLeaseSeconds { get; set; } = 300;
    public int OutboxRetentionDays { get; set; } = 14;
    public int RetryAttempts { get; set; } = 3;
    public int[] RetryDelaysSeconds { get; set; } = [30, 120, 600];
}
