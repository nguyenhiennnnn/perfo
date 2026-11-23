namespace Cinema.Application.Services.Inventory;

public static class InventoryLimits
{
    public const decimal MaxQuantity = 1_000_000m;
    public const decimal MaxUnitCost = 1_000_000_000m;
    public const int QuantityDecimals = 3;
    public const int MaxRecipeLines = 30;
    public const int MaxReceiptLines = 100;
    public const int MaxStocktakeLines = 200;
    public const int MaxWasteLines = 50;
}
