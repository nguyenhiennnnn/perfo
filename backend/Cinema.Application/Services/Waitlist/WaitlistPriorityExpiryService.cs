using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Waitlist;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Waitlist;

public class WaitlistPriorityExpiryService(
    IAppDbContext db,
    IWaitlistPriorityService priorityService,
    IWaitlistNotificationService notificationService,
    ILogger<WaitlistPriorityExpiryService> logger
) : IWaitlistPriorityExpiryService
{
    private const int BatchSize = 200;

    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var due = await db
            .WaitlistEntries.AsNoTracking()
            .Where(e =>
                e.Status == WaitlistStatus.Notified
                && e.PriorityExpiresAt != null
                && e.PriorityExpiresAt <= now
            )
            .OrderBy(e => e.PriorityExpiresAt)
            .ThenBy(e => e.WaitlistEntryId)
            .Select(e => new { e.WaitlistEntryId, e.ShowtimeId })
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var expired = 0;
        foreach (var entry in due)
        {
            try
            {
                if (
                    !await priorityService.EndAsync(
                        entry.WaitlistEntryId,
                        WaitlistStatus.Closed,
                        onlyIfDue: true,
                        cancellationToken
                    )
                )
                    continue;

                expired++;
                await notificationService.NotifyHeadAsync(entry.ShowtimeId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "WaitlistPriorityExpiryFailed WaitlistEntryId {WaitlistEntryId} ShowtimeId {ShowtimeId}",
                    entry.WaitlistEntryId,
                    entry.ShowtimeId
                );
            }
        }

        if (expired > 0)
            logger.LogInformation("WaitlistPriorityExpired Count {Count}", expired);

        return expired;
    }
}
