using Cinema.Application.Interfaces.Segments;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class CustomerSegmentRefreshJob(ICustomerSegmentService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 900)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.RefreshAsync(cancellationToken);
}
