using Cinema.Application.Interfaces.Campaigns;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class CampaignDispatchJob(ICampaignDispatchService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 900)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.DispatchDueAsync(cancellationToken);
}
