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

public class MembershipSummaryBlock(IAppDbContext db, IAppCache cache) : IReportBlock
{
    public string Key => "membership-summary";
    public string Title => Messages.Report_Finance_MembershipSummary_Title;
    public int Order => 69;

    private sealed class Bucket
    {
        public int ActiveMembers;
        public int RenewalDue;
        public int Renewed;
        public decimal Recognized;
        public decimal Deferred;
        public decimal Discount;
        public decimal FreeTickets;
    }

    private sealed record RenewalRow(int PlanId, bool Renewed);

    private sealed record AmountRow(int PlanId, decimal Amount);

    public async Task<bool> HasDataAsync(ReportPeriod period, CancellationToken cancellationToken)
    {
        var window = await WindowAsync(period, cancellationToken);
        return await MembershipRevenueLoader
                .Relevant(db, period, window.FromUtc, window.ToUtc)
                .AnyAsync(cancellationToken)
            || await Active(window.AsOfUtc).AnyAsync(cancellationToken)
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
        var (fromUtc, toUtc, asOfUtc) = await WindowAsync(period, cancellationToken);

        var revenue = await MembershipRevenueLoader.LoadAsync(
            db,
            period,
            fromUtc,
            toUtc,
            cancellationToken
        );

        var active = await Active(asOfUtc)
            .GroupBy(s => s.MembershipPlanId)
            .Select(g => new AmountRow(g.Key, g.Select(s => s.UserId).Distinct().Count()))
            .ToListAsync(cancellationToken);
        var totalActiveMembers = await Active(asOfUtc)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var renewals = await db
            .MembershipSubscriptions.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s =>
                s.Status != MembershipSubscriptionStatus.Refunded
                && s.ValidTo >= fromUtc
                && s.ValidTo < toUtc
            )
            .Select(s => new RenewalRow(
                s.MembershipPlanId,
                db.MembershipSubscriptions.IgnoreQueryFilters()
                    .Any(o =>
                        o.UserId == s.UserId
                        && o.MembershipSubscriptionId != s.MembershipSubscriptionId
                        && o.Status != MembershipSubscriptionStatus.Refunded
                        && o.ValidFrom >= s.ValidTo
                    )
            ))
            .ToListAsync(cancellationToken);

        var discounts = await db
            .OrderPriceAdjustments.AsNoTracking()
            .Where(a =>
                a.Type == PriceAdjustmentType.MembershipBenefit
                && a.ReferenceType == nameof(MembershipPlan)
                && a.ReferenceId != null
                && a.Order.Status == OrderStatus.Confirmed
                && a.Order.CreatedAt >= fromUtc
                && a.Order.CreatedAt < toUtc
            )
            .GroupBy(a => a.ReferenceId!.Value)
            .Select(g => new AmountRow(g.Key, g.Sum(a => a.Amount)))
            .ToListAsync(cancellationToken);

        var freeTickets = await db
            .MembershipFreeTickets.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t =>
                t.UsedAt != null
                && t.UsedAt >= fromUtc
                && t.UsedAt < toUtc
                && t.ConvertedAmount != null
            )
            .GroupBy(t => t.MembershipSubscription.MembershipPlanId)
            .Select(g => new AmountRow(g.Key, g.Sum(t => t.ConvertedAmount!.Value)))
            .ToListAsync(cancellationToken);

        var buckets = new Dictionary<int, Bucket>();
        Bucket For(int planId) =>
            buckets.TryGetValue(planId, out var b) ? b : buckets[planId] = new Bucket();

        foreach (var line in revenue)
        {
            var bucket = For(line.PlanId);
            bucket.Recognized = line.Recognized;
            bucket.Deferred = line.Closing;
        }
        foreach (var row in active)
            For(row.PlanId).ActiveMembers = (int)row.Amount;
        foreach (var group in renewals.GroupBy(r => r.PlanId))
        {
            var bucket = For(group.Key);
            bucket.RenewalDue = group.Count();
            bucket.Renewed = group.Count(r => r.Renewed);
        }
        foreach (var row in discounts)
            For(row.PlanId).Discount = row.Amount;
        foreach (var row in freeTickets)
            For(row.PlanId).FreeTickets = row.Amount;

        var planIds = buckets.Keys.ToList();
        var names = await db
            .MembershipPlans.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => planIds.Contains(p.MembershipPlanId))
            .ToDictionaryAsync(p => p.MembershipPlanId, p => p.Name, cancellationToken);

        var rows = buckets
            .Select(b => (Name: names.GetValueOrDefault(b.Key) ?? $"#{b.Key}", Bucket: b.Value))
            .OrderBy(b => b.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(b => ToRow(b.Name, b.Bucket))
            .ToList();

        var total = new Bucket
        {
            ActiveMembers = totalActiveMembers,
            RenewalDue = buckets.Values.Sum(b => b.RenewalDue),
            Renewed = buckets.Values.Sum(b => b.Renewed),
            Recognized = buckets.Values.Sum(b => b.Recognized),
            Deferred = buckets.Values.Sum(b => b.Deferred),
            Discount = buckets.Values.Sum(b => b.Discount),
            FreeTickets = buckets.Values.Sum(b => b.FreeTickets),
        };

        IReadOnlyList<ReportColumn> columns =
        [
            new("plan", Messages.Report_Finance_Col_MembershipPlan, ReportColumnType.Text),
            new("activeMembers", Messages.Report_Finance_Col_MembershipActiveMembers, ReportColumnType.Number),
            new("renewalDue", Messages.Report_Finance_Col_MembershipRenewalDue, ReportColumnType.Number),
            new("renewed", Messages.Report_Finance_Col_MembershipRenewed, ReportColumnType.Number),
            new("renewalRate", Messages.Report_Finance_Col_MembershipRenewalRate, ReportColumnType.Percent),
            new("recognized", Messages.Report_Finance_Col_MembershipRecognized, ReportColumnType.Money),
            new("deferred", Messages.Report_Finance_Col_MembershipDeferredClosing, ReportColumnType.Money),
            new("discount", Messages.Report_Finance_Col_MembershipBenefitDiscount, ReportColumnType.Money),
            new("freeTickets", Messages.Report_Finance_Col_MembershipBenefitFreeTickets, ReportColumnType.Money),
            new("benefitUsed", Messages.Report_Finance_Col_MembershipBenefitUsed, ReportColumnType.Money),
            new("benefitRatio", Messages.Report_Finance_Col_MembershipBenefitRatio, ReportColumnType.Percent),
            new("vsRetail", Messages.Report_Finance_Col_MembershipVsRetail, ReportColumnType.Money),
        ];

        return new ReportBlockResult(
            Key,
            Title,
            columns,
            rows,
            ToRow(Messages.Export_Total, total),
            [Messages.Report_Finance_MembershipSummary_Note]
        );
    }

    private IQueryable<MembershipSubscription> Active(DateTime asOfUtc) =>
        db.MembershipSubscriptions.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s =>
                s.ValidFrom <= asOfUtc
                && s.ValidTo > asOfUtc
                && (s.RefundedAt == null || s.RefundedAt > asOfUtc)
            );

    private async Task<(DateTime FromUtc, DateTime ToUtc, DateTime AsOfUtc)> WindowAsync(
        ReportPeriod period,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var toUtc = BusinessDay.EndUtc(period.To, settings.OpeningTime, settings.ClosingTime);
        var now = DateTime.UtcNow;
        return (
            BusinessDay.StartUtc(period.From, settings.OpeningTime, settings.ClosingTime),
            toUtc,
            toUtc < now ? toUtc : now
        );
    }

    private static IReadOnlyDictionary<string, object?> ToRow(string name, Bucket b)
    {
        var used = b.Discount + b.FreeTickets;
        return new Dictionary<string, object?>
        {
            ["plan"] = name,
            ["activeMembers"] = b.ActiveMembers,
            ["renewalDue"] = b.RenewalDue,
            ["renewed"] = b.Renewed,
            ["renewalRate"] = ReportMath.Percent(b.Renewed, b.RenewalDue),
            ["recognized"] = b.Recognized,
            ["deferred"] = b.Deferred,
            ["discount"] = b.Discount,
            ["freeTickets"] = b.FreeTickets,
            ["benefitUsed"] = used,
            ["benefitRatio"] = ReportMath.Percent(used, b.Recognized),
            ["vsRetail"] = b.Recognized - used,
        };
    }
}
