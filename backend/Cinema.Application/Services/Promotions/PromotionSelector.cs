using Cinema.Application.Caching;
using Cinema.Application.Utils;

namespace Cinema.Application.Services.Promotions;

public static class PromotionSelector
{
    public static PromotionSelection Select(
        IReadOnlyList<EligiblePromotion> eligible,
        IReadOnlyList<PromotionLine> lines
    )
    {
        var baseValues = lines.Select(l => Math.Max(l.Value, 0m)).ToArray();
        var ordered = eligible
            .Select(e =>
                (
                    Eligible: e,
                    Standalone: TryApply(
                        e.Promotion,
                        e.Usage,
                        lines,
                        (decimal[])baseValues.Clone(),
                        out _
                    )?.Amount ?? 0m
                )
            )
            .OrderByDescending(x => x.Eligible.Promotion.Priority)
            .ThenByDescending(x => x.Standalone)
            .ThenBy(x => x.Eligible.Promotion.PromotionId)
            .Select(x => x.Eligible)
            .ToList();

        var applied = new List<AppliedPromotion>();
        var rejected = new List<(PromotionSnapshot, PromotionRejectionReason)>();
        var remaining = (decimal[])baseValues.Clone();

        foreach (var candidate in ordered)
        {
            var promotion = candidate.Promotion;
            var conflicts =
                applied.Count > 0
                && (promotion.IsExclusive || applied.Any(a => a.Promotion.IsExclusive));

            if (!conflicts)
            {
                var result = TryApply(promotion, candidate.Usage, lines, remaining, out var reason);
                if (result is null)
                    rejected.Add((promotion, reason!.Value));
                else
                    applied.Add(result);
                continue;
            }

            var standalone = TryApply(
                promotion,
                candidate.Usage,
                lines,
                (decimal[])baseValues.Clone(),
                out var standaloneReason
            );
            if (standalone is null)
            {
                rejected.Add((promotion, standaloneReason!.Value));
                continue;
            }

            if (standalone.Amount <= applied.Sum(a => a.Amount))
            {
                rejected.Add((promotion, PromotionRejectionReason.NotCombinable));
                continue;
            }

            rejected.AddRange(applied.Select(a => (a.Promotion, PromotionRejectionReason.NotCombinable)));
            applied.Clear();
            remaining = (decimal[])baseValues.Clone();
            applied.Add(TryApply(promotion, candidate.Usage, lines, remaining, out _)!);
        }

        return new PromotionSelection(applied, rejected);
    }

    private static AppliedPromotion? TryApply(
        PromotionSnapshot promotion,
        PromotionUsage usage,
        IReadOnlyList<PromotionLine> lines,
        decimal[] remaining,
        out PromotionRejectionReason? reason
    )
    {
        var scopeValues = new decimal[remaining.Length];
        for (var i = 0; i < remaining.Length; i++)
            scopeValues[i] = PromotionConditions.InScope(promotion.Scope, lines[i].LineType)
                ? remaining[i]
                : 0m;

        var amount = PromotionConditions.ComputeDiscount(promotion, scopeValues.Sum());
        if (amount <= 0m)
        {
            reason = PromotionRejectionReason.NothingToDiscount;
            return null;
        }

        if (promotion.BudgetAmount is { } budget && usage.TotalAmount + amount > budget)
        {
            reason = PromotionRejectionReason.BudgetExhausted;
            return null;
        }

        var shares = DiscountAllocator.Allocate(amount, scopeValues);
        for (var i = 0; i < remaining.Length; i++)
            remaining[i] -= shares[i];

        reason = null;
        return new AppliedPromotion(promotion, shares.Sum(), shares);
    }
}
