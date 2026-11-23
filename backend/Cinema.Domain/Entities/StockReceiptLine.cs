namespace Cinema.Domain.Entities;

public class StockReceiptLine
{
    public int StockReceiptLineId { get; set; }
    public int StockReceiptId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public StockReceipt Receipt { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
