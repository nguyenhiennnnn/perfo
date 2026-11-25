using Cinema.Application.Interfaces.Loyalty;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class LoyaltyExpiryReminderJob(ILoyaltyExpiryReminderService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.SendDueAsync(cancellationToken);
}
