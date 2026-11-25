using Cinema.Application.Interfaces.Reports;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class SalesStatsRefreshJob(ISalesStatsService salesStats)
{
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        salesStats.RefreshRecentAsync(cancellationToken);
}
