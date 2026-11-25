using Cinema.Application.Interfaces.Import;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.BackgroundJobs;

public class CleanupImportSessionsJob(
    IImportSessionStore sessions,
    ILogger<CleanupImportSessionsJob> logger
)
{
    public Task RunAsync(CancellationToken cancellationToken)
    {
        var purged = sessions.PurgeExpired();
        if (purged > 0)
            logger.LogInformation("ImportSessionCleanup: đã xoá {Count} phiên xem trước hết hạn.", purged);

        return Task.CompletedTask;
    }
}
