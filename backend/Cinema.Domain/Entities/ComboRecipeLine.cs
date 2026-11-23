namespace Cinema.Domain.Entities;

public class ComboRecipeLine
{
    public int ComboRecipeLineId { get; set; }
    public int ComboRecipeId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal Quantity { get; set; }

    public ComboRecipe Recipe { get; set; } = null!;
    public InventoryItem InventoryItem { get; set; } = null!;
}
