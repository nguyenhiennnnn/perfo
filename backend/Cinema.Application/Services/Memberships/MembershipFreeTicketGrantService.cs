using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Memberships;

public class MembershipFreeTicketGrantService(
    IAppDbContext db,
    ILogger<MembershipFreeTicketGrantService> logger
) : IMembershipFreeTicketGrantService
{
    private const int LockClassId = 22;
    private const int BatchSize = 100;

    private sealed record DueSubscription(
        int SubscriptionId,
        int UserId,
        DateTime ValidFrom,
        DateTime ValidTo,
        int TicketsPerMonth
    );

    public async Task<int> GrantDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var granted = 0;
        var afterId = 0;
        while (true)
        {
            var due = await db
                .MembershipSubscriptions.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(s =>
                    s.MembershipSubscriptionId > afterId
                    && s.Status == MembershipSubscriptionStatus.Active
                    && s.ValidFrom <= now
                    && s.ValidTo > now
                    && s.MembershipPlan.FreeTicketsPerMonth > 0
                    && !db.MembershipFreeTickets.Any(t =>
                        t.MembershipSubscriptionId == s.MembershipSubscriptionId
                        && t.PeriodStart <= now
                        && t.PeriodEnd > now
                    )
                )
                .OrderBy(s => s.MembershipSubscriptionId)
                .Take(BatchSize)
                .Select(s => new DueSubscription(
                    s.MembershipSubscriptionId,
                    s.UserId,
                    s.ValidFrom,
                    s.ValidTo,
                    s.MembershipPlan.FreeTicketsPerMonth
                ))
                .ToListAsync(cancellationToken);
            if (due.Count == 0)
                break;

            afterId = due[^1].SubscriptionId;
            foreach (var subscription in due)
            {
                try
                {
                    granted += await GrantAsync(subscription, now, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(
                        ex,
                        "MembershipFreeTicketGrantFailed MembershipSubscriptionId {MembershipSubscriptionId}",
                        subscription.SubscriptionId
                    );
                }
            }
        }

        if (granted > 0)
            logger.LogInformation("MembershipFreeTicketsGranted Count {Count}", granted);
        return granted;
    }

    private async Task<int> GrantAsync(
        DueSubscription subscription,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var (index, start, end) = MembershipPlanRules.MonthPeriodAt(
            subscription.ValidFrom,
            subscription.ValidTo,
            now
        );

        var count = 0;
        await db.InTransactionAsync(
            async () =>
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({LockClassId}, {subscription.SubscriptionId})",
                    cancellationToken
                );

                if (
                    await db.MembershipFreeTickets.AnyAsync(
                        t =>
                            t.MembershipSubscriptionId == subscription.SubscriptionId
                            && t.PeriodIndex == index,
                        cancellationToken
                    )
                )
                    return;

                for (var sequence = 1; sequence <= subscription.TicketsPerMonth; sequence++)
                    db.MembershipFreeTickets.Add(
                        new MembershipFreeTicket
                        {
                            MembershipSubscriptionId = subscription.SubscriptionId,
                            UserId = subscription.UserId,
                            PeriodIndex = index,
                            Sequence = sequence,
                            PeriodStart = start,
                            PeriodEnd = end,
                            GrantedAt = now,
                        }
                    );
                await db.SaveChangesAsync(cancellationToken);
                count = subscription.TicketsPerMonth;
            },
            cancellationToken
        );
        return count;
    }
}
