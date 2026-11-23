namespace Cinema.Domain.Entities;

public class WasteSheetLine
{
    public int WasteSheetLineId { get; set; }
    public int WasteSheetId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public WasteSheet Sheet { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
