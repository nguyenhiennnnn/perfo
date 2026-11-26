using Cinema.Application.Interfaces.Memberships;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class MembershipExpiryJob(IMembershipExpiryService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.ExpireDueAsync(cancellationToken);
}
