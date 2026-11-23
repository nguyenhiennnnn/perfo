using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class OrderPricing
{
    public static decimal RoundToDong(decimal value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);

    public static decimal[] AllocateProportionally(decimal amount, IReadOnlyList<decimal> bases)
    {
        var result = new decimal[bases.Count];
        var total = bases.Sum();
        if (amount <= 0m || total <= 0m)
            return result;

        amount = Math.Min(RoundToDong(amount), total);
        var allocated = 0m;
        for (var i = 0; i < bases.Count; i++)
        {
            result[i] = Math.Min(bases[i], Math.Floor(amount * bases[i] / total));
            allocated += result[i];
        }

        for (var i = 0; allocated < amount && i < bases.Count; i++)
        {
            var extra = Math.Min(bases[i] - result[i], amount - allocated);
            result[i] += extra;
            allocated += extra;
        }

        return result;
    }

    public static decimal TotalAmount(IEnumerable<OrderLine> lines) =>
        lines.Sum(l => l.LockedUnitPrice * l.Quantity);

    public static decimal TierDiscountAmount(decimal ticketAmount, decimal discountPercent) =>
        Math.Min(RoundToDong(ticketAmount * discountPercent / 100m), ticketAmount);

    public static OrderPriceAdjustment BuildTierAdjustment(
        decimal amount,
        int? tierId,
        IReadOnlyList<OrderLine> ticketLines
    )
    {
        var shares = AllocateProportionally(
            amount,
            ticketLines.Select(l => l.LockedUnitPrice).ToList()
        );
        var adjustment = new OrderPriceAdjustment
        {
            Type = PriceAdjustmentType.TierDiscount,
            Amount = amount,
            ReferenceType = nameof(MembershipTier),
            ReferenceId = tierId,
        };
        for (var i = 0; i < ticketLines.Count; i++)
        {
            if (shares[i] > 0m)
                adjustment.Allocations.Add(
                    new OrderPriceAdjustmentAllocation
                    {
                        OrderLine = ticketLines[i],
                        Amount = shares[i],
                    }
                );
        }

        return adjustment;
    }

    public static void SettlePaidAmounts(Order order)
    {
        foreach (var line in order.Lines)
            line.PaidAmount = line.LockedUnitPrice * line.Quantity;
        foreach (var allocation in order.PriceAdjustments.SelectMany(a => a.Allocations))
            allocation.OrderLine.PaidAmount -= allocation.Amount;
    }

    public static void ApplyTotals(Order order)
    {
        SettlePaidAmounts(order);
        order.TotalAmount = TotalAmount(order.Lines);
        order.DiscountAmount = order.PriceAdjustments.Sum(a => a.Amount);
        order.FinalAmount = order.TotalAmount - order.DiscountAmount;
    }
}
