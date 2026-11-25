using Cinema.Application.Caching;
using Cinema.Application.DTOs.Waitlist;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Waitlist;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Waitlist;

public class WaitlistService(
    IAppDbContext db,
    IAppCache cache,
    IWaitlistPriorityService priorityService,
    IWaitlistNotificationService notificationService,
    ILogger<WaitlistService> logger
) : IWaitlistService
{
    private const int MaxPositionAttempts = 5;

    public async Task<WaitlistEntryDto> RegisterAsync(
        int userId,
        int showtimeId,
        RegisterWaitlistRequest request,
        CancellationToken cancellationToken
    )
    {
        var showtime =
            await db
                .Showtimes.AsNoTracking()
                .Where(s => s.ShowtimeId == showtimeId)
                .Select(s => new { s.Status, s.StartTime })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);

        if (showtime.Status == ShowtimeStatus.Cancelled)
            throw new BusinessRuleException(Messages.Rule_ShowtimeCancelled);
        if (showtime.Status == ShowtimeStatus.Completed)
            throw new BusinessRuleException(Messages.Rule_ShowtimeEnded);
        if (showtime.StartTime <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_WaitlistShowtimeStarted);

        var maxSeats = (await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken))
            .MaxSeatsPerBooking;
        if (request.SeatCount > maxSeats)
        {
            var message = string.Format(Messages.Validation_Seats_MaxPerBooking, maxSeats);
            throw new ValidationAppException(
                message,
                new Dictionary<string, string[]> { ["seatCount"] = [message] }
            );
        }

        if (await HasActiveEntryAsync(userId, showtimeId, cancellationToken))
            throw new ConflictAppException(Messages.Conflict_WaitlistAlreadyRegistered);

        var slots = await db.LoadSeatSlotsAsync(showtimeId, cancellationToken);
        var gapRuleMode = await db.LoadSeatGapRuleModeAsync(cancellationToken);
        if (
            WaitlistSeatAvailability.HasFeasibleSelection(
                slots,
                request.SeatCount,
                request.RequireAdjacent,
                gapRuleMode
            )
        )
            throw new BusinessRuleException(Messages.Rule_WaitlistSeatsStillAvailable);

        for (var attempt = 0; attempt < MaxPositionAttempts; attempt++)
        {
            var nextPosition =
                (
                    await db
                        .WaitlistEntries.Where(e => e.ShowtimeId == showtimeId)
                        .MaxAsync(e => (int?)e.Position, cancellationToken)
                    ?? 0
                ) + 1;

            var entry = new WaitlistEntry
            {
                ShowtimeId = showtimeId,
                UserId = userId,
                SeatCount = request.SeatCount,
                RequireAdjacent = request.RequireAdjacent,
                Position = nextPosition,
                Status = WaitlistStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
            };
            db.WaitlistEntries.Add(entry);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                db.WaitlistEntries.Remove(entry);
                if (await HasActiveEntryAsync(userId, showtimeId, CancellationToken.None))
                    throw new ConflictAppException(Messages.Conflict_WaitlistAlreadyRegistered);
                continue;
            }

            return await ToDtoAsync(entry, cancellationToken);
        }

        throw new ConflictAppException(Messages.Conflict_WaitlistBusy);
    }

    public async Task<WaitlistEntryDto> WithdrawAsync(
        int userId,
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var entryId =
            await db
                .WaitlistEntries.AsNoTracking()
                .Where(e =>
                    e.UserId == userId
                    && e.ShowtimeId == showtimeId
                    && (e.Status == WaitlistStatus.Waiting || e.Status == WaitlistStatus.Notified)
                )
                .Select(e => (int?)e.WaitlistEntryId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_WaitlistEntry);

        var now = DateTime.UtcNow;
        var updated = await db
            .WaitlistEntries.Where(e =>
                e.WaitlistEntryId == entryId && e.Status == WaitlistStatus.Waiting
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(x => x.Status, WaitlistStatus.Withdrawn)
                        .SetProperty(x => x.ClosedAt, now),
                cancellationToken
            );
        if (updated == 0)
        {
            if (
                !await priorityService.EndAsync(
                    entryId,
                    WaitlistStatus.Withdrawn,
                    onlyIfDue: false,
                    cancellationToken
                )
            )
                throw new ConflictAppException(Messages.Conflict_WaitlistNotWithdrawable);

            try
            {
                await notificationService.NotifyHeadAsync(showtimeId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "WaitlistHandoverFailed ShowtimeId {ShowtimeId} WaitlistEntryId {WaitlistEntryId}",
                    showtimeId,
                    entryId
                );
            }
        }

        var entry = await db
            .WaitlistEntries.AsNoTracking()
            .FirstAsync(e => e.WaitlistEntryId == entryId, cancellationToken);
        return await ToDtoAsync(entry, cancellationToken);
    }

    public async Task<WaitlistEntryDto> GetMineAsync(
        int userId,
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var entry =
            await db
                .WaitlistEntries.AsNoTracking()
                .Where(e => e.UserId == userId && e.ShowtimeId == showtimeId)
                .OrderByDescending(e => e.Position)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_WaitlistEntry);
        return await ToDtoAsync(entry, cancellationToken);
    }

    private Task<bool> HasActiveEntryAsync(
        int userId,
        int showtimeId,
        CancellationToken cancellationToken
    ) =>
        db.WaitlistEntries.AnyAsync(
            e =>
                e.UserId == userId
                && e.ShowtimeId == showtimeId
                && (
                    e.Status == WaitlistStatus.Waiting
                    || e.Status == WaitlistStatus.Notified
                    || e.Status == WaitlistStatus.Held
                ),
            cancellationToken
        );

    private async Task<WaitlistEntryDto> ToDtoAsync(
        WaitlistEntry entry,
        CancellationToken cancellationToken
    )
    {
        int? queuePosition = null;
        if (entry.Status is WaitlistStatus.Waiting or WaitlistStatus.Notified)
            queuePosition =
                await db
                    .WaitlistEntries.AsNoTracking()
                    .CountAsync(
                        e =>
                            e.ShowtimeId == entry.ShowtimeId
                            && e.Position < entry.Position
                            && (
                                e.Status == WaitlistStatus.Waiting
                                || e.Status == WaitlistStatus.Notified
                            ),
                        cancellationToken
                    ) + 1;

        return new WaitlistEntryDto(
            entry.WaitlistEntryId,
            entry.ShowtimeId,
            entry.SeatCount,
            entry.RequireAdjacent,
            entry.Status,
            queuePosition,
            entry.CreatedAt,
            entry.NotifiedAt,
            entry.ClosedAt,
            entry.PriorityExpiresAt
        );
    }
}
