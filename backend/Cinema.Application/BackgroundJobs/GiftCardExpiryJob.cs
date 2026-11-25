using Cinema.Application.Interfaces.GiftCards;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class GiftCardExpiryJob(IGiftCardExpiryService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.ExpireDueAsync(cancellationToken);
}
