using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class OrderPricing
{
    public static decimal RoundToDong(decimal value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);

    public static decimal[] AllocateProportionally(decimal amount, IReadOnlyList<decimal> bases) =>
        DiscountAllocator.Allocate(amount, bases);

    public static decimal TotalAmount(IEnumerable<OrderLine> lines) =>
        lines.Sum(l => l.LockedUnitPrice * l.Quantity);

    public static decimal PercentDiscountAmount(decimal baseAmount, decimal discountPercent) =>
        Math.Min(RoundToDong(baseAmount * discountPercent / 100m), baseAmount);

    public static decimal TierDiscountAmount(decimal ticketAmount, decimal discountPercent) =>
        PercentDiscountAmount(ticketAmount, discountPercent);

    public static OrderPriceAdjustment BuildAdjustment(
        PriceAdjustmentType type,
        string? referenceType,
        int? referenceId,
        IReadOnlyList<OrderLine> lines,
        IReadOnlyList<decimal> shares
    )
    {
        var adjustment = new OrderPriceAdjustment
        {
            Type = type,
            Amount = shares.Sum(),
            ReferenceType = referenceType,
            ReferenceId = referenceId,
        };
        for (var i = 0; i < lines.Count; i++)
        {
            if (shares[i] > 0m)
                adjustment.Allocations.Add(
                    new OrderPriceAdjustmentAllocation { OrderLine = lines[i], Amount = shares[i] }
                );
        }

        return adjustment;
    }

    public static OrderPriceAdjustment BuildTierAdjustment(
        decimal amount,
        int? tierId,
        IReadOnlyList<OrderLine> ticketLines
    )
    {
        var shares = DiscountAllocator.Allocate(
            amount,
            ticketLines.Select(l => l.LockedUnitPrice * l.Quantity).ToList()
        );
        return BuildAdjustment(
            PriceAdjustmentType.TierDiscount,
            nameof(MembershipTier),
            tierId,
            ticketLines,
            shares
        );
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
