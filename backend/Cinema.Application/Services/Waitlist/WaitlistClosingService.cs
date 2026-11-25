using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Waitlist;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Waitlist;

public class WaitlistClosingService(IAppDbContext db, ILogger<WaitlistClosingService> logger)
    : IWaitlistClosingService
{
    public async Task<int> CloseEndedAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var closed = await db
            .WaitlistEntries.Where(e =>
                (e.Status == WaitlistStatus.Waiting || e.Status == WaitlistStatus.Notified)
                && db.Showtimes.Any(s =>
                    s.ShowtimeId == e.ShowtimeId
                    && (s.Status != ShowtimeStatus.Scheduled || s.StartTime <= now)
                )
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.Status, WaitlistStatus.Closed).SetProperty(x => x.ClosedAt, now),
                cancellationToken
            );

        if (closed > 0)
            logger.LogInformation("WaitlistClosed Count {Count}", closed);

        return closed;
    }
}
