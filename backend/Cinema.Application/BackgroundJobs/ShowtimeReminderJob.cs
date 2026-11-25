using Cinema.Application.Interfaces.Orders;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class ShowtimeReminderJob(IShowtimeReminderService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.SendDueAsync(cancellationToken);
}
