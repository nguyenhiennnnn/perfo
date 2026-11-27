using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

internal sealed record MembershipRevenueLine(
    int PlanId,
    string Plan,
    int SoldCount,
    decimal SoldAmount,
    decimal Opening,
    decimal Recognized,
    decimal Converted,
    decimal Stopped,
    decimal Closing
);

internal static class MembershipRevenueLoader
{
    private sealed record SubscriptionRow(
        int SubscriptionId,
        int PlanId,
        DateTime CreatedAt,
        decimal Price,
        DateTime? RefundedAt,
        decimal Stopped
    );

    private sealed record EntryRow(int SubscriptionId, decimal Before, decimal InPeriod);

    public static IQueryable<MembershipSubscription> Relevant(
        IAppDbContext db,
        ReportPeriod period,
        DateTime fromUtc,
        DateTime toUtc
    ) =>
        db.MembershipSubscriptions.AsNoTracking()
            .Where(s =>
                s.CreatedAt < toUtc
                && (
                    s.CreatedAt >= fromUtc
                    || (
                        (s.RefundedAt == null || s.RefundedAt >= fromUtc)
                        && !db.MembershipRevenueEntries.Any(e =>
                            e.MembershipSubscriptionId == s.MembershipSubscriptionId
                            && e.PeriodEnd == s.ValidTo
                            && e.BusinessDate < period.From
                        )
                    )
                )
            );

    public static async Task<IReadOnlyList<MembershipRevenueLine>> LoadAsync(
        IAppDbContext db,
        ReportPeriod period,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken
    )
    {
        var relevant = Relevant(db, period, fromUtc, toUtc);

        var subscriptions = await relevant
            .Select(s => new SubscriptionRow(
                s.MembershipSubscriptionId,
                s.MembershipPlanId,
                s.CreatedAt,
                s.Order.Lines.Where(l =>
                        l.LineType == OrderLineType.Fee && l.MembershipPlanId != null
                    )
                    .Sum(l => l.LockedUnitPrice * l.Quantity),
                s.RefundedAt,
                s.StoppedRevenueAmount
            ))
            .ToListAsync(cancellationToken);

        var entries = await db
            .MembershipRevenueEntries.AsNoTracking()
            .Where(e =>
                e.BusinessDate <= period.To
                && relevant
                    .Select(s => s.MembershipSubscriptionId)
                    .Contains(e.MembershipSubscriptionId)
            )
            .GroupBy(e => e.MembershipSubscriptionId)
            .Select(g => new EntryRow(
                g.Key,
                g.Sum(e => e.BusinessDate < period.From ? e.Amount : 0m),
                g.Sum(e => e.BusinessDate >= period.From ? e.Amount : 0m)
            ))
            .ToListAsync(cancellationToken);
        var entryBySubscription = entries.ToDictionary(e => e.SubscriptionId);

        var converted = await db
            .MembershipRevenueConversions.AsNoTracking()
            .Where(c => c.BusinessDate >= period.From && c.BusinessDate <= period.To)
            .GroupBy(c => c.MembershipPlanId)
            .Select(g => new { PlanId = g.Key, Amount = g.Sum(c => c.Amount) })
            .ToDictionaryAsync(x => x.PlanId, x => x.Amount, cancellationToken);

        var planNames = await db
            .MembershipPlans.IgnoreQueryFilters()
            .AsNoTracking()
            .ToDictionaryAsync(p => p.MembershipPlanId, p => p.Name, cancellationToken);

        var bySubscriptionPlan = subscriptions.ToLookup(s => s.PlanId);
        return bySubscriptionPlan
            .Select(g => g.Key)
            .Union(converted.Keys)
            .Select(planId =>
            {
                var g = bySubscriptionPlan[planId].ToList();
                var sold = g.Where(s => s.CreatedAt >= fromUtc).ToList();
                var opening = g.Where(s => s.CreatedAt < fromUtc)
                    .Sum(s =>
                        s.Price
                        - Entry(entryBySubscription, s.SubscriptionId).Before
                        - (s.RefundedAt < fromUtc ? s.Stopped : 0m)
                    );
                var recognized = g.Sum(s => Entry(entryBySubscription, s.SubscriptionId).InPeriod);
                var stopped = g.Where(s => s.RefundedAt >= fromUtc && s.RefundedAt < toUtc)
                    .Sum(s => s.Stopped);
                var soldAmount = sold.Sum(s => s.Price);
                return new MembershipRevenueLine(
                    planId,
                    planNames.GetValueOrDefault(planId) ?? string.Empty,
                    sold.Count,
                    soldAmount,
                    opening,
                    recognized,
                    converted.GetValueOrDefault(planId),
                    stopped,
                    opening + soldAmount - recognized - stopped
                );
            })
            .Where(l =>
                l.SoldCount > 0
                || l.Opening != 0m
                || l.Recognized != 0m
                || l.Converted != 0m
                || l.Stopped != 0m
                || l.Closing != 0m
            )
            .OrderBy(l => l.Plan, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static EntryRow Entry(Dictionary<int, EntryRow> map, int subscriptionId) =>
        map.TryGetValue(subscriptionId, out var row) ? row : new EntryRow(subscriptionId, 0m, 0m);
}
