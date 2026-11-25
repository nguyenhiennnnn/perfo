using Cinema.Application.Interfaces.Events;
using Hangfire;

namespace Cinema.Application.BackgroundJobs;

public class ProcessOutboxJob(IOutboxProcessor processor)
{
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await processor.ProcessDueAsync(cancellationToken);
        await processor.PurgeProcessedAsync(cancellationToken);
    }
}
