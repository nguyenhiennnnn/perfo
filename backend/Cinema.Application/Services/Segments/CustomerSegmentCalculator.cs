using Cinema.Application.Utils;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Segments;

public sealed record SegmentRules(
    int NewCustomerDays,
    int LapsedDays,
    int RegularMinOrders,
    int LookbackDays,
    int HighValueMinScore,
    int LtvProjectionMonths,
    int TierUpgradePercent,
    int PreferenceMinViewings,
    int PreferenceMinSharePercent
);

public sealed record SegmentTierInput(int TierId, int MinPoints);

public sealed record SegmentCustomerInput(
    int UserId,
    DateTime CreatedAt,
    DateTime? FirstOrderAt,
    DateTime? LastOrderAt,
    int LookbackOrderCount,
    decimal LookbackSpent,
    decimal TotalSpent,
    int ActivePoints,
    int ExpiringPoints,
    DateTime? PointsExpireAt,
    IReadOnlyList<IReadOnlyList<int>> GenresPerViewing,
    IReadOnlyList<int> ScreenTypesPerViewing
);

public sealed record SegmentResult(
    int UserId,
    CustomerActivityStage ActivityStage,
    DateTime? FirstOrderAt,
    DateTime? LastOrderAt,
    int? DaysSinceLastOrder,
    int LookbackOrderCount,
    decimal LookbackSpent,
    decimal TotalSpent,
    int RecencyScore,
    int FrequencyScore,
    int MonetaryScore,
    bool IsHighSpender,
    bool IsHighFrequency,
    decimal EstimatedLifetimeValue,
    int? PreferredGenreId,
    int? PreferredScreenTypeId,
    int ExpiringPoints,
    DateTime? PointsExpireAt,
    int? NextTierId,
    int? PointsToNextTier,
    bool IsNearTierUpgrade
);

public static class CustomerSegmentCalculator
{
    private const int ScoreLevels = 5;
    private const int MinProjectionWindowDays = 30;
    private const decimal DaysPerMonth = 30m;

    public static List<SegmentResult> Calculate(
        IReadOnlyList<SegmentCustomerInput> customers,
        IReadOnlyList<SegmentTierInput> tiers,
        SegmentRules rules,
        DateTime now
    )
    {
        var active = customers.Where(c => c.LookbackOrderCount > 0).ToList();
        var daysByUser = customers.ToDictionary(c => c.UserId, c => DaysSince(c.LastOrderAt, now));

        var recencyDays = active.Select(c => (decimal)(daysByUser[c.UserId] ?? 0)).ToList();
        var frequencies = active.Select(c => (decimal)c.LookbackOrderCount).ToList();
        var spends = active.Select(c => c.LookbackSpent).ToList();

        var orderedTiers = tiers.OrderBy(t => t.MinPoints).ToList();

        return customers
            .Select(c =>
            {
                var isActive = c.LookbackOrderCount > 0;
                var days = daysByUser[c.UserId];
                var recency = isActive ? Score(recencyDays, days ?? 0, higherIsBetter: false) : 1;
                var frequency = isActive
                    ? Score(frequencies, c.LookbackOrderCount, higherIsBetter: true)
                    : 1;
                var monetary = isActive ? Score(spends, c.LookbackSpent, higherIsBetter: true) : 1;

                var stage = StageOf(c, now, rules);
                var (nextTierId, toNext, nearUpgrade) = NextTier(
                    c.ActivePoints,
                    orderedTiers,
                    rules.TierUpgradePercent
                );

                return new SegmentResult(
                    c.UserId,
                    stage,
                    c.FirstOrderAt,
                    c.LastOrderAt,
                    days,
                    c.LookbackOrderCount,
                    c.LookbackSpent,
                    c.TotalSpent,
                    recency,
                    frequency,
                    monetary,
                    isActive && c.LookbackSpent > 0 && monetary >= rules.HighValueMinScore,
                    isActive && frequency >= rules.HighValueMinScore,
                    LifetimeValue(c, stage, rules, now),
                    PickPreferred(
                        c.GenresPerViewing.SelectMany(g => g.Distinct()),
                        c.GenresPerViewing.Count(g => g.Count > 0),
                        rules
                    ),
                    PickPreferred(c.ScreenTypesPerViewing, c.ScreenTypesPerViewing.Count, rules),
                    c.ExpiringPoints,
                    c.ExpiringPoints > 0 ? c.PointsExpireAt : null,
                    nextTierId,
                    toNext,
                    nearUpgrade
                );
            })
            .ToList();
    }

    private static CustomerActivityStage StageOf(
        SegmentCustomerInput customer,
        DateTime now,
        SegmentRules rules
    )
    {
        var joinedAt = customer.FirstOrderAt ?? customer.CreatedAt;
        if (joinedAt >= now.AddDays(-rules.NewCustomerDays))
            return CustomerActivityStage.New;
        if (customer.LastOrderAt is { } last && last < now.AddDays(-rules.LapsedDays))
            return CustomerActivityStage.Lapsed;
        if (customer.LookbackOrderCount >= rules.RegularMinOrders)
            return CustomerActivityStage.Regular;
        return CustomerActivityStage.Casual;
    }

    private static int? DaysSince(DateTime? at, DateTime now) =>
        at is { } value ? Math.Max(0, (int)Math.Floor((now - value).TotalDays)) : null;

    private static int Score(IReadOnlyList<decimal> population, decimal value, bool higherIsBetter)
    {
        var worse = higherIsBetter
            ? population.Count(v => v < value)
            : population.Count(v => v > value);
        return Math.Min(ScoreLevels, 1 + worse * ScoreLevels / population.Count);
    }

    private static decimal LifetimeValue(
        SegmentCustomerInput customer,
        CustomerActivityStage stage,
        SegmentRules rules,
        DateTime now
    )
    {
        var history = Math.Max(0m, customer.TotalSpent);
        if (stage == CustomerActivityStage.Lapsed || rules.LtvProjectionMonths == 0)
            return OrderPricing.RoundToDong(history);

        var tenureDays = Math.Max(
            0d,
            (now - (customer.FirstOrderAt ?? customer.CreatedAt)).TotalDays
        );
        var windowDays = Math.Min(
            (decimal)rules.LookbackDays,
            Math.Max((decimal)MinProjectionWindowDays, (decimal)Math.Floor(tenureDays))
        );
        var monthly = Math.Max(0m, customer.LookbackSpent) * DaysPerMonth / windowDays;
        return OrderPricing.RoundToDong(history + monthly * rules.LtvProjectionMonths);
    }

    private static int? PickPreferred(IEnumerable<int> keys, int viewingCount, SegmentRules rules)
    {
        if (viewingCount < rules.PreferenceMinViewings)
            return null;

        var top = keys
            .GroupBy(k => k)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .FirstOrDefault();
        if (top is null || top.Count < rules.PreferenceMinViewings)
            return null;

        return top.Count * 100 >= viewingCount * rules.PreferenceMinSharePercent ? top.Key : null;
    }

    private static (int? NextTierId, int? PointsToNext, bool IsNear) NextTier(
        int points,
        IReadOnlyList<SegmentTierInput> orderedTiers,
        int upgradePercent
    )
    {
        var next = orderedTiers.FirstOrDefault(t => t.MinPoints > points);
        if (next is null)
            return (null, null, false);

        var remaining = next.MinPoints - points;
        return (next.TierId, remaining, remaining * 100 <= next.MinPoints * upgradePercent);
    }
}
