namespace Cinema.Domain.Entities;

public class InventoryItem
{
    public int InventoryItemId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public decimal QuantityOnHand { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LowStockThreshold { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ComboRecipeLine> RecipeLines { get; set; } = [];
    public ICollection<StockLedger> LedgerEntries { get; set; } = [];
}
