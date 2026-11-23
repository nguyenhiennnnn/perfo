namespace Cinema.Domain.Entities;

public class StockReceipt
{
    public int StockReceiptId { get; set; }
    public string SupplierName { get; set; } = null!;
    public DateOnly ReceivedDate { get; set; }
    public string? Note { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<StockReceiptLine> Lines { get; set; } = [];
}
