using Cinema.Application.Caching;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Memberships;

public class MembershipRevenueService(
    IAppDbContext db,
    IAppCache cache,
    IAuditLogger auditLogger,
    ILogger<MembershipRevenueService> logger
) : IMembershipRevenueService
{
    private const int LockClassId = 24;
    private const int BatchSize = 100;

    private sealed record DueSubscription(
        int SubscriptionId,
        DateTime ValidFrom,
        DateTime ValidTo,
        decimal Price
    );

    public async Task<int> RecognizeDueAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var businessDate = BusinessDay.Of(now, settings.OpeningTime, settings.ClosingTime);
        if (
            await db
                .DailyClosings.AsNoTracking()
                .AnyAsync(
                    c => c.BusinessDate == businessDate && c.Status == DailyClosingStatus.Closed,
                    cancellationToken
                )
        )
        {
            logger.LogWarning(
                "MembershipRevenueRecognitionSkipped BusinessDate {BusinessDate} Closed",
                businessDate
            );
            return 0;
        }

        var total = 0;
        var afterId = 0;
        while (true)
        {
            var due = await ProjectDue(
                    db.MembershipSubscriptions.AsNoTracking()
                        .Where(s =>
                            s.MembershipSubscriptionId > afterId
                            && s.ValidFrom <= now
                            && s.Status != MembershipSubscriptionStatus.Refunded
                            && s.Order.Status == OrderStatus.Confirmed
                            && !db.MembershipRevenueEntries.Any(e =>
                                e.MembershipSubscriptionId == s.MembershipSubscriptionId
                                && e.PeriodEnd == s.ValidTo
                            )
                        )
                        .OrderBy(s => s.MembershipSubscriptionId)
                        .Take(BatchSize)
                )
                .ToListAsync(cancellationToken);
            if (due.Count == 0)
                break;

            afterId = due[^1].SubscriptionId;
            foreach (var subscription in due)
            {
                try
                {
                    total += await RecognizeAsync(subscription, businessDate, now, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(
                        ex,
                        "MembershipRevenueRecognitionFailed MembershipSubscriptionId {MembershipSubscriptionId}",
                        subscription.SubscriptionId
                    );
                }
            }
        }

        if (total > 0)
            logger.LogInformation("MembershipRevenueRecognized Count {Count}", total);
        return total;
    }

    public async Task<MembershipRevenueSettlement> SettleForRefundAsync(
        int subscriptionId,
        CancellationToken cancellationToken
    )
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({LockClassId}, {subscriptionId})",
            cancellationToken
        );

        var subscription =
            await ProjectDue(
                    db.MembershipSubscriptions.AsNoTracking()
                        .IgnoreQueryFilters()
                        .Where(s => s.MembershipSubscriptionId == subscriptionId)
                )
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_MembershipSubscription);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;
        var businessDate = BusinessDay.Of(now, settings.OpeningTime, settings.ClosingTime);
        var closed = await db
            .DailyClosings.AsNoTracking()
            .AnyAsync(
                c => c.BusinessDate == businessDate && c.Status == DailyClosingStatus.Closed,
                cancellationToken
            );
        if (!closed && subscription.ValidFrom <= now)
            await RecognizeAsync(subscription, businessDate, now, cancellationToken);

        var months = MembershipRevenueSchedule.Months(subscription.ValidFrom, subscription.ValidTo);
        var amounts = MembershipRevenueSchedule.Split(subscription.Price, months);
        var recorded = await db
            .MembershipRevenueEntries.AsNoTracking()
            .Where(e => e.MembershipSubscriptionId == subscriptionId)
            .Select(e => e.MonthIndex)
            .ToListAsync(cancellationToken);
        var remaining = Enumerable.Range(0, months).Where(i => !recorded.Contains(i)).ToList();
        return new MembershipRevenueSettlement(
            months,
            remaining.Count,
            remaining.Sum(i => amounts[i])
        );
    }

    private static IQueryable<DueSubscription> ProjectDue(IQueryable<MembershipSubscription> query) =>
        query.Select(s => new DueSubscription(
            s.MembershipSubscriptionId,
            s.ValidFrom,
            s.ValidTo,
            s.Order.Lines.Where(l => l.LineType == OrderLineType.Fee && l.MembershipPlanId != null)
                .Sum(l => l.LockedUnitPrice * l.Quantity)
        ));

    private async Task<int> RecognizeAsync(
        DueSubscription subscription,
        DateOnly businessDate,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var months = MembershipRevenueSchedule.Months(subscription.ValidFrom, subscription.ValidTo);
        var amounts = MembershipRevenueSchedule.Split(subscription.Price, months);

        var count = 0;
        await db.InTransactionAsync(
            async () =>
            {
                count = 0;
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({LockClassId}, {subscription.SubscriptionId})",
                    cancellationToken
                );

                var recorded = await db
                    .MembershipRevenueEntries.Where(e =>
                        e.MembershipSubscriptionId == subscription.SubscriptionId
                    )
                    .Select(e => e.MonthIndex)
                    .ToListAsync(cancellationToken);

                for (var index = 0; index < months; index++)
                {
                    if (recorded.Contains(index))
                        continue;

                    var (start, end) = MembershipRevenueSchedule.Period(
                        subscription.ValidFrom,
                        subscription.ValidTo,
                        months,
                        index
                    );
                    if (start > now)
                        break;

                    db.MembershipRevenueEntries.Add(
                        new MembershipRevenueEntry
                        {
                            MembershipSubscriptionId = subscription.SubscriptionId,
                            MonthIndex = index,
                            PeriodStart = start,
                            PeriodEnd = end,
                            BusinessDate = businessDate,
                            Amount = amounts[index],
                            OccurredAt = now,
                        }
                    );
                    auditLogger.Record(
                        AuditActions.RecognizeMembershipRevenue,
                        AuditEntityTypes.MembershipSubscription,
                        subscription.SubscriptionId.ToString(),
                        null,
                        new
                        {
                            monthIndex = index,
                            businessDate,
                            amount = amounts[index],
                        }
                    );
                    count++;
                }

                if (count > 0)
                    await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );
        return count;
    }
}
