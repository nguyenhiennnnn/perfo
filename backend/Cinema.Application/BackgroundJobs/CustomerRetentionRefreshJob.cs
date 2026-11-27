using Cinema.Application.Interfaces.Reports;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class CustomerRetentionRefreshJob(IRetentionStatsService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 900)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.RefreshAsync(cancellationToken);
}
