using Cinema.Application.Interfaces;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class GiftCardExpiryJob(IGiftCardExpiryService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.ExpireDueAsync(cancellationToken);
}
