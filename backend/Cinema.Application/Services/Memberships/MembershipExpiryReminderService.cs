using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Interfaces.Notifications;
using Cinema.Application.Interfaces.Users;
using Cinema.Application.Options;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Memberships;

public class MembershipExpiryReminderService(
    IAppDbContext db,
    IAppCache cache,
    ICommunicationPreferenceService preferences,
    INotificationService notificationService,
    IOptions<AppUrlOptions> appUrlOptions,
    ILogger<MembershipExpiryReminderService> logger
) : IMembershipExpiryReminderService
{
    private const int BatchSize = 100;

    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var windowEnd = now.AddDays(settings.MembershipExpiryReminderDays);

        var sent = 0;
        var afterId = 0;
        while (true)
        {
            var ids = await DueSubscriptions(now, windowEnd)
                .Where(s => s.MembershipSubscriptionId > afterId)
                .OrderBy(s => s.MembershipSubscriptionId)
                .Select(s => s.MembershipSubscriptionId)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (ids.Count == 0)
                break;

            afterId = ids[^1];
            foreach (var id in ids)
            {
                try
                {
                    sent += await RemindAsync(id, now, windowEnd, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(
                        ex,
                        "MembershipExpiryReminderFailed MembershipSubscriptionId {MembershipSubscriptionId}",
                        id
                    );
                }
            }
        }

        if (sent > 0)
            logger.LogInformation("MembershipExpiryRemindersSent Count {Count}", sent);
        return sent;
    }

    private IQueryable<MembershipSubscription> DueSubscriptions(DateTime now, DateTime windowEnd) =>
        db.MembershipSubscriptions.Where(s =>
            s.Status == MembershipSubscriptionStatus.Active
            && s.ExpiryReminderSentAt == null
            && s.ValidFrom <= now
            && s.ValidTo > now
            && s.ValidTo <= windowEnd
            && !db.MembershipSubscriptions.Any(n =>
                n.UserId == s.UserId
                && n.Status == MembershipSubscriptionStatus.Active
                && n.ValidTo > s.ValidTo
            )
        );

    private async Task<int> RemindAsync(
        int subscriptionId,
        DateTime now,
        DateTime windowEnd,
        CancellationToken cancellationToken
    )
    {
        var due = await DueSubscriptions(now, windowEnd)
            .Where(s => s.MembershipSubscriptionId == subscriptionId)
            .Select(s => new
            {
                s.UserId,
                s.ValidTo,
                PlanName = s.MembershipPlan.Name,
                s.User.Email,
                s.User.FullName,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (due is null || string.IsNullOrWhiteSpace(due.Email))
            return 0;

        if (
            !await preferences.CanSendAsync(
                due.UserId,
                EmailCategory.ReminderAndReviewInvite,
                cancellationToken
            )
        )
            return 0;

        var claimed = await db
            .MembershipSubscriptions.Where(s =>
                s.MembershipSubscriptionId == subscriptionId
                && s.ExpiryReminderSentAt == null
                && s.Status == MembershipSubscriptionStatus.Active
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.ExpiryReminderSentAt, (DateTime?)now),
                cancellationToken
            );
        if (claimed == 0)
            return 0;

        notificationService.SendMembershipExpiryReminder(
            due.Email,
            new MembershipExpiryReminderEmailData(
                due.FullName,
                due.PlanName,
                due.ValidTo,
                $"{appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/')}/membership"
            )
        );
        return 1;
    }
}
