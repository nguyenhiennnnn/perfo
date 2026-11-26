using Cinema.Application.Interfaces.Memberships;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class MembershipFreeTicketGrantJob(IMembershipFreeTicketGrantService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.GrantDueAsync(cancellationToken);
}
