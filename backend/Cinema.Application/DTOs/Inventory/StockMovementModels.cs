using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Inventory;

public sealed record StockMovement(
    int InventoryItemId,
    StockMovementType MovementType,
    decimal QuantityChange,
    string? ReferenceType = null,
    int? ReferenceId = null,
    string? Reason = null,
    decimal? NewUnitCost = null
);

public sealed record StockPosting(long StockLedgerId, decimal BalanceAfter, decimal UnitCost);

public sealed record StockReceiptCostChange(
    int InventoryItemId,
    decimal QuantityBefore,
    decimal CostBefore,
    decimal CostAfter
);
