using Cinema.Application.BackgroundJobs;
using Cinema.Application.Options;
using Cinema.Infrastructure.Persistence;
using Cinema.Shared.Constants;
using Hangfire;
using Hangfire.PostgreSql;

namespace Cinema.Api.Extensions;

public static class BackgroundJobExtensions
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        if (environment.IsEnvironment("Testing"))
            return services;

        var connectionString = configuration.GetPostgresConnectionString();
        var options = configuration.BindSection<BackgroundJobOptions>(
            BackgroundJobOptions.SectionName
        );

        services.AddHangfire(config =>
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString))
        );
        services.AddHangfireServer();
        GlobalJobFilters.Filters.Add(
            new AutomaticRetryAttribute
            {
                Attempts = options.RetryAttempts,
                DelaysInSeconds = options.RetryDelaysSeconds,
            }
        );

        return services;
    }

    public static WebApplication UseRecurringJobs(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
            return app;

        var options = app.Configuration.BindSection<BackgroundJobOptions>(
            BackgroundJobOptions.SectionName
        );
        var jobOptions = new RecurringJobOptions
        {
            TimeZone = Cinema.Application.Utils.BusinessTime.Zone,
        };

        RecurringJob.AddOrUpdate<ReleaseExpiredSeatsJob>(
            RecurringJobIds.ReleaseExpiredSeats,
            j => j.RunAsync(CancellationToken.None),
            options.ReleaseExpiredSeatsCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<ShowtimeLifecycleJob>(
            RecurringJobIds.UpdateShowtimeLifecycle,
            j => j.RunAsync(CancellationToken.None),
            options.ShowtimeLifecycleCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<ExpireLoyaltyPointsJob>(
            RecurringJobIds.ExpireLoyaltyPoints,
            j => j.RunAsync(CancellationToken.None),
            options.ExpireLoyaltyPointsCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<LoyaltyExpiryReminderJob>(
            RecurringJobIds.LoyaltyExpiryReminder,
            j => j.RunAsync(CancellationToken.None),
            options.LoyaltyExpiryReminderCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<CleanupImportExportFilesJob>(
            RecurringJobIds.CleanupImportExportFiles,
            j => j.RunAsync(CancellationToken.None),
            options.CleanupImportExportFilesCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<CleanupImportSessionsJob>(
            RecurringJobIds.CleanupImportSessions,
            j => j.RunAsync(CancellationToken.None),
            options.CleanupImportSessionsCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<LowStockAlertJob>(
            RecurringJobIds.LowStockAlert,
            j => j.RunAsync(CancellationToken.None),
            options.LowStockAlertCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<ProcessOutboxJob>(
            RecurringJobIds.ProcessOutbox,
            j => j.RunAsync(CancellationToken.None),
            options.ProcessOutboxCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<SalesStatsRefreshJob>(
            RecurringJobIds.RefreshSalesStats,
            j => j.RunAsync(CancellationToken.None),
            options.SalesStatsRefreshCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<UnclosedShiftAlertJob>(
            RecurringJobIds.UnclosedShiftAlert,
            j => j.RunAsync(CancellationToken.None),
            options.UnclosedShiftAlertCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<GiftCardDispatchJob>(
            RecurringJobIds.GiftCardDispatch,
            j => j.RunAsync(CancellationToken.None),
            options.GiftCardDispatchCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<GiftCardExpiryJob>(
            RecurringJobIds.GiftCardExpiry,
            j => j.RunAsync(CancellationToken.None),
            options.GiftCardExpiryCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<ShowtimeReminderJob>(
            RecurringJobIds.ShowtimeReminder,
            j => j.RunAsync(CancellationToken.None),
            options.ShowtimeReminderCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<MovieAffinityRefreshJob>(
            RecurringJobIds.RefreshMovieAffinity,
            j => j.RunAsync(CancellationToken.None),
            options.MovieAffinityRefreshCron,
            jobOptions
        );
        RecurringJob.AddOrUpdate<MovieIntroEmailJob>(
            RecurringJobIds.MovieIntroEmail,
            j => j.RunAsync(CancellationToken.None),
            options.MovieIntroEmailCron,
            jobOptions
        );

        return app;
    }
}
