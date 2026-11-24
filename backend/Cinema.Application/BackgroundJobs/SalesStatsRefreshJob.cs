using Cinema.Application.Interfaces;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class SalesStatsRefreshJob(ISalesStatsService salesStats)
{
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        salesStats.RefreshRecentAsync(cancellationToken);
}
