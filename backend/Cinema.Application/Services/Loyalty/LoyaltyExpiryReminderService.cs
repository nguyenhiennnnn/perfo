using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Users;
using Cinema.Application.Options;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Loyalty;

public class LoyaltyExpiryReminderService(
    IAppDbContext db,
    IAppCache cache,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<LoyaltyExpiryReminderService> logger
) : ILoyaltyExpiryReminderService
{
    private const int BatchSize = 100;

    private sealed record DueLot(int EntryId, int Points, DateTime ExpiresAt);

    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var windowEnd = now.AddDays(settings.PointsExpiryReminderDays);

        var sent = 0;
        var afterUserId = 0;
        while (true)
        {
            var userIds = await DueEntries(now, windowEnd)
                .Where(e => e.UserId > afterUserId)
                .Select(e => e.UserId)
                .Distinct()
                .OrderBy(id => id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (userIds.Count == 0)
                break;

            afterUserId = userIds[^1];
            foreach (var userId in userIds)
            {
                try
                {
                    sent += await RemindUserAsync(userId, now, windowEnd, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "PointsExpiryReminderFailed UserId {UserId}", userId);
                }
            }
        }

        if (sent > 0)
            logger.LogInformation("PointsExpiryRemindersSent Count {Count}", sent);
        return sent;
    }

    private IQueryable<LoyaltyPointEntry> DueEntries(DateTime now, DateTime windowEnd) =>
        db.LoyaltyPointEntries.Where(e =>
            e.RemainingPoints > 0
            && e.ExpiryReminderSentAt == null
            && e.ExpiresAt > now
            && e.ExpiresAt <= windowEnd
        );

    private async Task<int> RemindUserAsync(
        int userId,
        DateTime now,
        DateTime windowEnd,
        CancellationToken cancellationToken
    )
    {
        var recipient = await db
            .Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Email, u.FullName })
            .FirstOrDefaultAsync(cancellationToken);
        if (recipient is null || string.IsNullOrWhiteSpace(recipient.Email))
            return 0;

        if (
            !await preferences.CanSendAsync(
                userId,
                EmailCategory.ReminderAndReviewInvite,
                cancellationToken
            )
        )
            return 0;

        var lots = await DueEntries(now, windowEnd)
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.ExpiresAt)
            .ThenBy(e => e.EntryId)
            .Select(e => new DueLot(e.EntryId, e.RemainingPoints, e.ExpiresAt))
            .ToListAsync(cancellationToken);
        if (lots.Count == 0)
            return 0;

        var entryIds = lots.Select(l => l.EntryId).ToList();
        var claimed = await db
            .LoyaltyPointEntries.Where(e =>
                entryIds.Contains(e.EntryId) && e.ExpiryReminderSentAt == null
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.ExpiryReminderSentAt, (DateTime?)now),
                cancellationToken
            );
        if (claimed == 0)
            return 0;

        notificationService.SendPointsExpiryReminder(
            recipient.Email,
            new PointsExpiryReminderEmailData(
                recipient.FullName,
                lots.Sum(l => l.Points),
                $"{appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/')}/account",
                [.. lots.Select(l => new PointsExpiryReminderLine(l.Points, l.ExpiresAt))]
            )
        );
        return 1;
    }
}
