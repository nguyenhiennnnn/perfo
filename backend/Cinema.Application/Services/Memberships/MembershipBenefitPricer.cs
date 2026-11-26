using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Services.Orders;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Memberships;

public class MembershipBenefitPricer(IAppDbContext db) : IMembershipBenefitPricer
{
    public async Task<IReadOnlyList<MembershipBenefitDiscount>> ComputeAsync(
        MembershipBenefitContext context,
        CancellationToken cancellationToken
    )
    {
        if (context.Order.UserId is not { } userId)
            return [];

        var lines = context.Lines;
        if (!lines.Any(l => l.LineType is OrderLineType.Ticket or OrderLineType.Combo))
            return [];

        var now = DateTime.UtcNow;
        var plan = await db
            .MembershipSubscriptions.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s =>
                s.UserId == userId
                && s.Status == MembershipSubscriptionStatus.Active
                && s.ValidFrom <= now
                && s.ValidTo > now
            )
            .OrderByDescending(s => s.ValidFrom)
            .Select(s => new
            {
                s.MembershipPlanId,
                s.MembershipPlan.TicketDiscountPercent,
                s.MembershipPlan.ComboDiscountPercent,
                s.MembershipPlan.StackableWithPromotions,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (plan is null)
            return [];

        var shares = new decimal[lines.Count];
        AddShares(shares, lines, context.RemainingValues, OrderLineType.Ticket, plan.TicketDiscountPercent);
        AddShares(shares, lines, context.RemainingValues, OrderLineType.Combo, plan.ComboDiscountPercent);
        if (shares.Sum() <= 0m)
            return [];

        return
        [
            new MembershipBenefitDiscount(
                shares,
                nameof(MembershipPlan),
                plan.MembershipPlanId,
                !plan.StackableWithPromotions
            ),
        ];
    }

    private static void AddShares(
        decimal[] shares,
        IReadOnlyList<OrderLine> lines,
        IReadOnlyList<decimal> remaining,
        OrderLineType lineType,
        decimal percent
    )
    {
        if (percent <= 0m)
            return;

        var bases = lines
            .Select((l, i) => l.LineType == lineType ? Math.Max(remaining[i], 0m) : 0m)
            .ToArray();
        var amount = OrderPricing.PercentDiscountAmount(bases.Sum(), percent);
        if (amount <= 0m)
            return;

        var allocated = DiscountAllocator.Allocate(amount, bases);
        for (var i = 0; i < shares.Length; i++)
            shares[i] += allocated[i];
    }
}
