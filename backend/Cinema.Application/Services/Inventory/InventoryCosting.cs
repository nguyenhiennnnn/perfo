using Cinema.Application.Utils;

namespace Cinema.Application.Services.Inventory;

public static class InventoryCosting
{
    public static decimal WeightedAverageCost(
        decimal quantityOnHand,
        decimal currentCost,
        decimal receivedQuantity,
        decimal receivedCost
    )
    {
        var total = quantityOnHand + receivedQuantity;
        if (quantityOnHand <= 0 || total <= 0)
            return OrderPricing.RoundToDong(receivedCost);

        return OrderPricing.RoundToDong(
            (quantityOnHand * currentCost + receivedQuantity * receivedCost) / total
        );
    }

    public static decimal StockValue(decimal quantity, decimal unitCost) =>
        OrderPricing.RoundToDong(quantity * unitCost);
}
