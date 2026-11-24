using System.Globalization;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.ImportExport;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.BackgroundJobs;

public class SalesStatsRebuildJob(
    IServiceScopeFactory scopeFactory,
    IImportExportJobTracker tracker,
    ILogger<SalesStatsRebuildJob> logger
)
{
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public async Task RunAsync(
        Guid jobId,
        string fromDate,
        string toDate,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var context = await tracker.MarkRunningAsync(jobId, cancellationToken);
            if (context is null)
            {
                logger.LogWarning("SalesStatsRebuildJob: JobId {JobId} không tồn tại.", jobId);
                return;
            }

            var from = DateOnly.ParseExact(
                fromDate,
                SalesStatsRebuildService.DateFormat,
                CultureInfo.InvariantCulture
            );
            var to = DateOnly.ParseExact(
                toDate,
                SalesStatsRebuildService.DateFormat,
                CultureInfo.InvariantCulture
            );

            var progress = new JobProgressReporter(tracker, jobId, context.TotalRows, 1);
            var processed = 0;
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using var scope = scopeFactory.CreateAsyncScope();
                var salesStats = scope.ServiceProvider.GetRequiredService<ISalesStatsService>();
                await salesStats.RebuildDayAsync(day, cancellationToken);

                processed++;
                await progress.ReportAsync(processed, cancellationToken);
            }

            await tracker.CompleteImportAsync(jobId, processed, 0, null, CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "SalesStatsRebuildJob thất bại JobId {JobId}", jobId);
            await tracker.FailAsync(
                jobId,
                ImportExportJobErrors.Describe(ex),
                CancellationToken.None
            );
            throw;
        }
    }
}
