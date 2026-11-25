using Cinema.Application.Interfaces.Waitlist;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class WaitlistCloseJob(IWaitlistClosingService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) => service.CloseEndedAsync(cancellationToken);
}
