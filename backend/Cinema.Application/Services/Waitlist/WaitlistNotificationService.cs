using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Users;
using Cinema.Application.Interfaces.Waitlist;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Waitlist;

public class WaitlistNotificationService(
    IAppDbContext db,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<WaitlistNotificationService> logger
) : IWaitlistNotificationService
{
    private sealed record Candidate(
        int WaitlistEntryId,
        int UserId,
        int SeatCount,
        bool RequireAdjacent,
        string Email,
        string FullName
    );

    private sealed record ShowtimeInfo(string MovieTitle, DateTime StartTime, string RoomName);

    private sealed record Claimed(Candidate Entry, ShowtimeInfo Showtime);

    public async Task<bool> NotifyHeadAsync(int showtimeId, CancellationToken cancellationToken)
    {
        Claimed? claimed = null;
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
                    link
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
            await db
                .WaitlistEntries.Where(e =>
                    e.WaitlistEntryId == claimed.Entry.WaitlistEntryId
                    && e.Status == WaitlistStatus.Notified
                )
                .ExecuteUpdateAsync(
                    s =>
                        s.SetProperty(x => x.Status, WaitlistStatus.Waiting)
                            .SetProperty(x => x.NotifiedAt, (DateTime?)null),
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

        foreach (var entry in entries)
        {
            if (
                !WaitlistSeatAvailability.HasFeasibleSelection(
                    slots,
                    entry.SeatCount,
                    entry.RequireAdjacent,
                    gapRuleMode
                )
            )
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
                            .SetProperty(x => x.NotifiedAt, now),
                    cancellationToken
                );
            if (updated == 0)
                return null;

            return new Claimed(
                new Candidate(
                    entry.WaitlistEntryId,
                    entry.UserId,
                    entry.SeatCount,
                    entry.RequireAdjacent,
                    entry.Email,
                    entry.FullName
                ),
                showtime
            );
        }

        return null;
    }
}
