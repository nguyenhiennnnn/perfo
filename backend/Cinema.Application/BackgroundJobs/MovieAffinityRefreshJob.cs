using Cinema.Application.Interfaces.Recommendations;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class MovieAffinityRefreshJob(IMovieAffinityService service)
{
    [DisableConcurrentExecution(timeoutInSeconds: 900)]
    public Task RunAsync(CancellationToken cancellationToken) =>
        service.RefreshAsync(cancellationToken);
}
