using Cinema.Application.Interfaces.Surveys;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class RoomQualityAlertJob(IRoomQualityAlertService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    public Task RunAsync(CancellationToken cancellationToken) => service.RunAsync(cancellationToken);
}
