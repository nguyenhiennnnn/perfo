using Cinema.Application.Caching;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Reports.Finance;

public class MembershipRevenueBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "membership-revenue";
    public string Title => Messages.Report_Finance_MembershipRevenue_Title;
    public int Order => 68;

    private sealed record SubscriptionRow(
        int SubscriptionId,
        int PlanId,
        DateTime CreatedAt,
        decimal Price
    );

    private sealed record EntryRow(int SubscriptionId, decimal Before, decimal InPeriod);

    private sealed record Line(
        string Plan,
        int SoldCount,
        decimal SoldAmount,
        decimal Opening,
        decimal Recognized,
        decimal Converted,
        decimal Closing
    );

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = await RangeAsync(period, cancellationToken);
        return await Relevant(period, fromUtc, toUtc).AnyAsync(cancellationToken)
            || await db
                .MembershipRevenueConversions.AsNoTracking()
                .AnyAsync(
                    c => c.BusinessDate >= period.From && c.BusinessDate <= period.To,
                    cancellationToken
                );
    }

    public async Task<ReportBlockResult> BuildAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var (fromUtc, toUtc) = await RangeAsync(period, cancellationToken);
        var relevant = Relevant(period, fromUtc, toUtc);

        var subscriptions = await relevant
            .Select(s => new SubscriptionRow(
                s.MembershipSubscriptionId,
                s.MembershipPlanId,
                s.CreatedAt,
                s.Order.Lines.Where(l =>
                        l.LineType == OrderLineType.Fee && l.MembershipPlanId != null
                    )
                    .Sum(l => l.LockedUnitPrice * l.Quantity)
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
        var lines = bySubscriptionPlan
            .Select(g => g.Key)
            .Union(converted.Keys)
            .Select(planId =>
            {
                var g = bySubscriptionPlan[planId].ToList();
                var sold = g.Where(s => s.CreatedAt >= fromUtc).ToList();
                var opening = g.Where(s => s.CreatedAt < fromUtc)
                    .Sum(s => s.Price - Entry(entryBySubscription, s.SubscriptionId).Before);
                var recognized = g.Sum(s => Entry(entryBySubscription, s.SubscriptionId).InPeriod);
                var soldAmount = sold.Sum(s => s.Price);
                return new Line(
                    planNames.GetValueOrDefault(planId) ?? string.Empty,
                    sold.Count,
                    soldAmount,
                    opening,
                    recognized,
                    converted.GetValueOrDefault(planId),
                    opening + soldAmount - recognized
                );
            })
            .Where(l =>
                l.SoldCount > 0
                || l.Opening != 0m
                || l.Recognized != 0m
                || l.Converted != 0m
                || l.Closing != 0m
            )
            .OrderBy(l => l.Plan, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        IReadOnlyList<ReportColumn> columns =
        [
            new("plan", Messages.Report_Finance_Col_MembershipPlan, ReportColumnType.Text),
            new("soldCount", Messages.Report_Finance_Col_MembershipSoldCount, ReportColumnType.Number),
            new("soldAmount", Messages.Report_Finance_Col_MembershipSoldAmount, ReportColumnType.Money),
            new("opening", Messages.Report_Finance_Col_MembershipDeferredOpening, ReportColumnType.Money),
            new("recognized", Messages.Report_Finance_Col_MembershipRecognized, ReportColumnType.Money),
            new("converted", Messages.Report_Finance_Col_MembershipConverted, ReportColumnType.Money),
            new("net", Messages.Report_Finance_Col_MembershipNetRevenue, ReportColumnType.Money),
            new("closing", Messages.Report_Finance_Col_MembershipDeferredClosing, ReportColumnType.Money),
        ];

        var rows = lines
            .Select(l =>
                (IReadOnlyDictionary<string, object?>)
                    new Dictionary<string, object?>
                    {
                        ["plan"] = l.Plan,
                        ["soldCount"] = l.SoldCount,
                        ["soldAmount"] = l.SoldAmount,
                        ["opening"] = l.Opening,
                        ["recognized"] = l.Recognized,
                        ["converted"] = l.Converted,
                        ["net"] = l.Recognized - l.Converted,
                        ["closing"] = l.Closing,
                    }
            )
            .ToList();

        var totals = new Dictionary<string, object?>
        {
            ["plan"] = Messages.Export_Total,
            ["soldCount"] = lines.Sum(l => l.SoldCount),
            ["soldAmount"] = lines.Sum(l => l.SoldAmount),
            ["opening"] = lines.Sum(l => l.Opening),
            ["recognized"] = lines.Sum(l => l.Recognized),
            ["converted"] = lines.Sum(l => l.Converted),
            ["net"] = lines.Sum(l => l.Recognized - l.Converted),
            ["closing"] = lines.Sum(l => l.Closing),
        };

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            totals,
            [Messages.Report_Finance_MembershipRevenue_Note]
        );
    }

    private IQueryable<MembershipSubscription> Relevant(
        ReportPeriod period,
        DateTime fromUtc,
        DateTime toUtc
    ) =>
        db.MembershipSubscriptions.AsNoTracking()
            .Where(s =>
                s.CreatedAt < toUtc
                && (
                    s.CreatedAt >= fromUtc
                    || !db.MembershipRevenueEntries.Any(e =>
                        e.MembershipSubscriptionId == s.MembershipSubscriptionId
                        && e.PeriodEnd == s.ValidTo
                        && e.BusinessDate < period.From
                    )
                )
            );

    private static EntryRow Entry(Dictionary<int, EntryRow> map, int subscriptionId) =>
        map.TryGetValue(subscriptionId, out var row) ? row : new EntryRow(subscriptionId, 0m, 0m);

    private async Task<(DateTime FromUtc, DateTime ToUtc)> RangeAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        return (
            BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime),
            BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime)
        );
    }
}
