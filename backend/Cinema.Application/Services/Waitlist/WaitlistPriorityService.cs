using Cinema.Application.Interfaces.Bookings;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Waitlist;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Waitlist;

public class WaitlistPriorityService(
    IAppDbContext db,
    ISeatBlockService seatBlockService,
    IRealtimeNotifier realtimeNotifier
) : IWaitlistPriorityService
{
    public async Task<List<int>> ConsumeAsync(
        int userId,
        int showtimeId,
        IReadOnlyCollection<int> requestedSeatIds,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var entryId = await db
            .WaitlistEntries.AsNoTracking()
            .Where(e =>
                e.ShowtimeId == showtimeId
                && e.UserId == userId
                && e.Status == WaitlistStatus.Notified
                && e.PriorityExpiresAt > now
            )
            .Select(e => (int?)e.WaitlistEntryId)
            .FirstOrDefaultAsync(cancellationToken);
        if (entryId is null)
            return [];

        var reservedSeatIds = await db
            .SeatBlocks.AsNoTracking()
            .Where(b => b.WaitlistEntryId == entryId && b.Type == SeatBlockType.WaitlistReserve)
            .Select(b => b.ShowtimeSeatId)
            .ToListAsync(cancellationToken);
        if (!reservedSeatIds.Intersect(requestedSeatIds).Any())
            return [];

        var claimed = await db
            .WaitlistEntries.Where(e =>
                e.WaitlistEntryId == entryId
                && e.Status == WaitlistStatus.Notified
                && e.PriorityExpiresAt > now
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.Status, WaitlistStatus.Held),
                cancellationToken
            );
        if (claimed == 0)
            return [];

        return await seatBlockService.ReleaseWaitlistReserveAsync(
            showtimeId,
            [entryId.Value],
            cancellationToken
        );
    }

    public async Task<bool> EndAsync(
        int waitlistEntryId,
        WaitlistStatus newStatus,
        bool onlyIfDue,
        CancellationToken cancellationToken
    )
    {
        var showtimeId = await db
            .WaitlistEntries.AsNoTracking()
            .Where(e => e.WaitlistEntryId == waitlistEntryId)
            .Select(e => (int?)e.ShowtimeId)
            .FirstOrDefaultAsync(cancellationToken);
        if (showtimeId is null)
            return false;

        var now = DateTime.UtcNow;
        var ended = false;
        var releasedSeatIds = new List<int>();
        await db.InTransactionAsync(
            async () =>
            {
                var query = db.WaitlistEntries.Where(e =>
                    e.WaitlistEntryId == waitlistEntryId && e.Status == WaitlistStatus.Notified
                );
                if (onlyIfDue)
                    query = query.Where(e => e.PriorityExpiresAt != null && e.PriorityExpiresAt <= now);

                var updated =
                    newStatus == WaitlistStatus.Waiting
                        ? await query.ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(x => x.Status, WaitlistStatus.Waiting)
                                    .SetProperty(x => x.NotifiedAt, (DateTime?)null)
                                    .SetProperty(x => x.PriorityExpiresAt, (DateTime?)null),
                            cancellationToken
                        )
                        : await query.ExecuteUpdateAsync(
                            s =>
                                s.SetProperty(x => x.Status, newStatus)
                                    .SetProperty(x => x.ClosedAt, now),
                            cancellationToken
                        );
                if (updated == 0)
                    return;

                ended = true;
                releasedSeatIds = await seatBlockService.ReleaseWaitlistReserveAsync(
                    showtimeId.Value,
                    [waitlistEntryId],
                    cancellationToken
                );
            },
            cancellationToken
        );

        if (releasedSeatIds.Count > 0)
            await realtimeNotifier.NotifySeatReleasedAsync(
                showtimeId.Value,
                releasedSeatIds,
                cancellationToken
            );

        return ended;
    }
}
