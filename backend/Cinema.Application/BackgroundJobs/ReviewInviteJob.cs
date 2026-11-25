using Cinema.Application.Interfaces.Reviews;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class ReviewInviteJob(IReviewInviteService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.SendDueAsync(cancellationToken);
}
