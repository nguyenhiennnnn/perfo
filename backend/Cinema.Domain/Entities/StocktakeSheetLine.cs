namespace Cinema.Domain.Entities;

public class StocktakeSheetLine
{
    public int StocktakeSheetLineId { get; set; }
    public int StocktakeSheetId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal BookQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Difference { get; set; }

    public StocktakeSheet Sheet { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
