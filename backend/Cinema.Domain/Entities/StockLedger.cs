using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class StockLedger
{
    public long StockLedgerId { get; set; }
    public int InventoryItemId { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal QuantityChange { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Reason { get; set; }
    public int? ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }

    public InventoryItem InventoryItem { get; set; } = null!;
}
