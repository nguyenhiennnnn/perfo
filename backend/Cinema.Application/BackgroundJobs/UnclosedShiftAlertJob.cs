using Cinema.Application.Interfaces;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class UnclosedShiftAlertJob(IUnclosedShiftAlertService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) => service.RunAsync(cancellationToken);
}
