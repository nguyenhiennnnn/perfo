using System.Linq.Expressions;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Inventory;

public static class InventoryStockRules
{
    public static readonly Expression<Func<InventoryItem, bool>> IsLowStockExpression = i =>
        i.LowStockThreshold > 0 && i.QuantityOnHand <= i.LowStockThreshold;

    public static readonly Expression<Func<InventoryItem, bool>> IsAboveThresholdExpression = i =>
        i.LowStockThreshold <= 0 || i.QuantityOnHand > i.LowStockThreshold;

    public static bool IsLowStock(decimal quantityOnHand, decimal lowStockThreshold) =>
        lowStockThreshold > 0 && quantityOnHand <= lowStockThreshold;
}
