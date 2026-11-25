using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Cinema.Application.Interfaces.Orders;


namespace Cinema.Application.Utils;

public record RefundComboShare(decimal PaidAmount, int Quantity, int QuantityFulfilled);

public record RefundChargeCapacity(
    int ChargeTransactionId,
    PaymentMethod Method,
    decimal Refundable,
    decimal GatewayFee
);

public static class OrderRefundCalculator
{
    public static OrderRefundPlan BuildPlan(
        int orderId,
        RefundScope scope,
        IReadOnlyList<RefundComboShare> combos,
        IReadOnlyList<RefundChargeCapacity> charges,
        decimal? refundableCap = null
    )
    {
        var percent = scope.Kind == RefundScopeKind.Full ? 100m : scope.Percent;
        if (percent <= 0m || percent > 100m)
            throw new BusinessRuleException(Messages.Refund_PercentInvalid);

        var capacity = TotalCapacity(charges);
        if (refundableCap is { } cap)
            capacity = Math.Min(capacity, Math.Max(0m, cap));
        var fulfilled = scope.DeductFulfilledCombo
            ? Math.Min(capacity, FulfilledComboAmount(combos))
            : 0m;
        var basis = capacity - fulfilled;
        var total = Math.Min(capacity, OrderPricing.RoundToDong(basis * percent / 100m));
        return new OrderRefundPlan(orderId, total, fulfilled, Allocate(charges, total));
    }

    public static OrderRefundPlan BuildPlanForAmount(
        int orderId,
        decimal amount,
        IReadOnlyList<RefundChargeCapacity> charges
    )
    {
        amount = OrderPricing.RoundToDong(amount);
        if (amount <= 0m)
            throw new BusinessRuleException(Messages.Refund_AmountInvalid);
        if (amount > TotalCapacity(charges))
            throw new BusinessRuleException(Messages.Refund_ExceedsCollected);

        return new OrderRefundPlan(orderId, amount, 0m, Allocate(charges, amount));
    }

    public static decimal FulfilledComboAmount(IEnumerable<RefundComboShare> combos) =>
        combos
            .Where(c => c.Quantity > 0 && c.QuantityFulfilled > 0)
            .Sum(c =>
                OrderPricing.RoundToDong(
                    c.PaidAmount * Math.Min(c.QuantityFulfilled, c.Quantity) / c.Quantity
                )
            );

    public static decimal NetRefundable(decimal refundable, decimal gatewayFee) =>
        Math.Floor(Math.Max(0m, refundable - gatewayFee));

    public static decimal TotalCapacity(IEnumerable<RefundChargeCapacity> charges) =>
        charges.Sum(c => NetRefundable(c.Refundable, c.GatewayFee));

    public static IReadOnlyList<RefundPlanItem> Allocate(
        IReadOnlyList<RefundChargeCapacity> charges,
        decimal total
    )
    {
        var amounts = AllocateAmounts(
            total,
            [.. charges.Select(c => NetRefundable(c.Refundable, c.GatewayFee))]
        );
        return
        [
            .. charges
                .Select((c, i) => new RefundPlanItem(c.ChargeTransactionId, c.Method, amounts[i]))
                .Where(i => i.Amount > 0m),
        ];
    }

    public static decimal[] AllocateAmounts(decimal total, IReadOnlyList<decimal> capacities)
    {
        var result = new decimal[capacities.Count];
        var capacity = capacities.Sum();
        if (total <= 0m || capacity <= 0m)
            return result;

        total = Math.Min(OrderPricing.RoundToDong(total), capacity);
        for (var i = 0; i < capacities.Count; i++)
            result[i] = Math.Min(
                capacities[i],
                OrderPricing.RoundToDong(total * capacities[i] / capacity)
            );

        var difference = total - result.Sum();
        foreach (var i in Enumerable.Range(0, capacities.Count).OrderByDescending(k => capacities[k]))
        {
            if (difference == 0m)
                break;

            var step =
                difference > 0m
                    ? Math.Min(difference, capacities[i] - result[i])
                    : -Math.Min(-difference, result[i]);
            result[i] += step;
            difference -= step;
        }

        return result;
    }

    public static int RefundableUnits(OrderLine line) =>
        line.Quantity
        - line.RefundedQuantity
        - (line.LineType == OrderLineType.Combo ? line.QuantityFulfilled : 0);

    public static decimal LineRefundAmount(
        decimal paidAmount,
        int quantity,
        int refundedQuantity,
        int units,
        decimal percent
    ) =>
        OrderPricing.RoundToDong(
            (
                CumulativeLineValue(paidAmount, quantity, refundedQuantity + units)
                - CumulativeLineValue(paidAmount, quantity, refundedQuantity)
            )
                * percent
                / 100m
        );

    public static decimal LoyaltyReversalBase(
        decimal loyaltyEarnedAmount,
        decimal orderFinalAmount,
        decimal refundedAmount
    ) =>
        orderFinalAmount <= 0m
            ? 0m
            : Math.Min(
                loyaltyEarnedAmount,
                OrderPricing.RoundToDong(loyaltyEarnedAmount * refundedAmount / orderFinalAmount)
            );

    private static decimal CumulativeLineValue(decimal paidAmount, int quantity, int units) =>
        units >= quantity ? paidAmount : OrderPricing.RoundToDong(paidAmount * units / quantity);
}
