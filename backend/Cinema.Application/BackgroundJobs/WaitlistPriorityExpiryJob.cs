using Cinema.Application.Interfaces.Waitlist;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class WaitlistPriorityExpiryJob(IWaitlistPriorityExpiryService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.ExpireDueAsync(cancellationToken);
}
