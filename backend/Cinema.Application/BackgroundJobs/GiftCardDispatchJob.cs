using Cinema.Application.Interfaces.GiftCards;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class GiftCardDispatchJob(IGiftCardDispatchService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.DispatchDueAsync(cancellationToken);
}
