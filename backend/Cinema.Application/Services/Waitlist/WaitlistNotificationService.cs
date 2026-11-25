using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Bookings;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Users;
using Cinema.Application.Interfaces.Waitlist;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Waitlist;

public class WaitlistNotificationService(
    IAppDbContext db,
    IAppCache cache,
    ISeatBlockService seatBlockService,
    IWaitlistPriorityService priorityService,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<WaitlistNotificationService> logger
) : IWaitlistNotificationService
{
    private const int MaxClaimAttempts = 3;

    private sealed record Candidate(
        int WaitlistEntryId,
        int UserId,
        int SeatCount,
        bool RequireAdjacent,
        string Email,
        string FullName
    );

    private sealed record ShowtimeInfo(string MovieTitle, DateTime StartTime, string RoomName);

    private sealed record Claimed(Candidate Entry, ShowtimeInfo Showtime, DateTime PriorityUntil);

    public async Task<bool> NotifyHeadAsync(int showtimeId, CancellationToken cancellationToken)
    {
        Claimed? claimed = null;
        for (var attempt = 0; attempt < MaxClaimAttempts; attempt++)
        {
            try
            {
                await db.InTransactionAsync(
                    async () =>
                    {
                        await db.Database.ExecuteSqlInterpolatedAsync(
                            $"SELECT 1 FROM \"Showtimes\" WHERE \"ShowtimeId\" = {showtimeId} FOR NO KEY UPDATE",
                            cancellationToken
                        );
                        claimed = await ClaimHeadAsync(showtimeId, cancellationToken);
                    },
                    cancellationToken
                );
                break;
            }
            catch (SeatConflictException)
            {
                claimed = null;
            }
        }

        if (claimed is null)
            return false;

        try
        {
            var link = $"{appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/')}/booking/{showtimeId}";
            notificationService.SendWaitlistSeatsAvailable(
                claimed.Entry.Email,
                new WaitlistSeatsAvailableEmailData(
                    claimed.Entry.FullName,
                    claimed.Showtime.MovieTitle,
                    claimed.Showtime.StartTime,
                    claimed.Showtime.RoomName,
                    claimed.Entry.SeatCount,
                    link,
                    claimed.PriorityUntil
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "WaitlistNoticeFailed ShowtimeId {ShowtimeId} WaitlistEntryId {WaitlistEntryId}",
                showtimeId,
                claimed.Entry.WaitlistEntryId
            );
            await priorityService.EndAsync(
                claimed.Entry.WaitlistEntryId,
                WaitlistStatus.Waiting,
                onlyIfDue: false,
                CancellationToken.None
            );
            throw;
        }

        logger.LogInformation(
            "WaitlistNotified ShowtimeId {ShowtimeId} WaitlistEntryId {WaitlistEntryId}",
            showtimeId,
            claimed.Entry.WaitlistEntryId
        );
        return true;
    }

    private async Task<Claimed?> ClaimHeadAsync(int showtimeId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var showtime = await db
            .Showtimes.AsNoTracking()
            .Where(s =>
                s.ShowtimeId == showtimeId
                && s.Status == ShowtimeStatus.Scheduled
                && s.StartTime > now
            )
            .Select(s => new ShowtimeInfo(s.Movie.Title, s.StartTime, s.Room.RoomName))
            .FirstOrDefaultAsync(cancellationToken);
        if (showtime is null)
            return null;

        var entries = await db
            .WaitlistEntries.AsNoTracking()
            .Where(e =>
                e.ShowtimeId == showtimeId
                && (e.Status == WaitlistStatus.Waiting || e.Status == WaitlistStatus.Notified)
            )
            .OrderBy(e => e.Position)
            .Select(e => new
            {
                e.WaitlistEntryId,
                e.UserId,
                e.SeatCount,
                e.RequireAdjacent,
                e.Status,
                e.User.Email,
                e.User.FullName,
            })
            .ToListAsync(cancellationToken);
        if (
            entries.Count == 0
            || entries.Any(e => e.Status == WaitlistStatus.Notified)
        )
            return null;

        var slots = await db.LoadSeatSlotsAsync(showtimeId, cancellationToken);
        var gapRuleMode = await db.LoadSeatGapRuleModeAsync(cancellationToken);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var priorityUntil = now.AddMinutes(settings.WaitlistPriorityMinutes);
        if (priorityUntil > showtime.StartTime)
            priorityUntil = showtime.StartTime;

        foreach (var entry in entries)
        {
            var seatIds = WaitlistSeatAvailability.FindSelection(
                slots,
                entry.SeatCount,
                entry.RequireAdjacent,
                gapRuleMode
            );
            if (seatIds is null)
                continue;

            if (
                string.IsNullOrWhiteSpace(entry.Email)
                || !await preferences.CanSendAsync(
                    entry.UserId,
                    EmailCategory.Service,
                    cancellationToken
                )
            )
                continue;

            var updated = await db
                .WaitlistEntries.Where(e =>
                    e.WaitlistEntryId == entry.WaitlistEntryId
                    && e.Status == WaitlistStatus.Waiting
                )
                .ExecuteUpdateAsync(
                    s =>
                        s.SetProperty(x => x.Status, WaitlistStatus.Notified)
                            .SetProperty(x => x.NotifiedAt, now)
                            .SetProperty(x => x.PriorityExpiresAt, priorityUntil),
                    cancellationToken
                );
            if (updated == 0)
                return null;

            await seatBlockService.BlockSeatsAsync(
                showtimeId,
                seatIds,
                SeatBlockType.WaitlistReserve,
                Messages.SeatBlock_WaitlistReserveReason,
                priorityUntil,
                cancellationToken,
                entry.WaitlistEntryId
            );

            return new Claimed(
                new Candidate(
                    entry.WaitlistEntryId,
                    entry.UserId,
                    entry.SeatCount,
                    entry.RequireAdjacent,
                    entry.Email,
                    entry.FullName
                ),
                showtime,
                priorityUntil
            );
        }

        return null;
    }
}
