using Cinema.Application.DTOs.Inventory;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Inventory;

public static class InventoryMapper
{
    public static InventoryItemDto ToDto(InventoryItem i) =>
        new(
            i.InventoryItemId,
            i.Code,
            i.Name,
            i.Unit,
            i.QuantityOnHand,
            i.UnitCost,
            InventoryCosting.StockValue(i.QuantityOnHand, i.UnitCost),
            i.LowStockThreshold,
            InventoryStockRules.IsLowStock(i.QuantityOnHand, i.LowStockThreshold),
            i.IsActive,
            i.CreatedAt,
            i.UpdatedAt
        );

    public static StockLevelDto ToStockLevel(InventoryItem i) =>
        new(
            i.InventoryItemId,
            i.Code,
            i.Name,
            i.Unit,
            i.QuantityOnHand,
            i.LowStockThreshold,
            InventoryStockRules.IsLowStock(i.QuantityOnHand, i.LowStockThreshold)
        );

    public static InventoryValuationItemDto ToValuationItem(InventoryItem i) =>
        new(
            i.InventoryItemId,
            i.Code,
            i.Name,
            i.Unit,
            i.QuantityOnHand,
            i.UnitCost,
            InventoryCosting.StockValue(i.QuantityOnHand, i.UnitCost)
        );

    public static ComboRecipeDto ToDto(ComboRecipe recipe, string comboName) =>
        new(
            recipe.ComboId,
            comboName,
            [
                .. recipe
                    .Lines.OrderBy(l => l.InventoryItem.Name)
                    .ThenBy(l => l.InventoryItemId)
                    .Select(l => new ComboRecipeLineDto(
                        l.InventoryItemId,
                        l.InventoryItem.Code,
                        l.InventoryItem.Name,
                        l.InventoryItem.Unit,
                        l.Quantity
                    )),
            ],
            recipe.CreatedAt,
            recipe.UpdatedAt
        );

    public static decimal LineAmount(StockReceiptLine line) =>
        InventoryCosting.StockValue(line.Quantity, line.UnitCost);

    public static StockReceiptListItemDto ToListItem(StockReceipt r) =>
        new(
            r.StockReceiptId,
            r.SupplierName,
            r.ReceivedDate,
            r.Lines.Count,
            r.Lines.Sum(l => LineAmount(l)),
            r.CreatedAt
        );

    public static decimal LineAmount(WasteSheetLine line) =>
        InventoryCosting.StockValue(line.Quantity, line.UnitCost);

    public static StocktakeSheetListItemDto ToListItem(StocktakeSheet s) =>
        new(
            s.StocktakeSheetId,
            s.StocktakeDate,
            s.Note,
            s.Lines.Count,
            s.Lines.Sum(l => l.Difference),
            s.CreatedAt
        );

    public static StocktakeSheetDto ToDto(StocktakeSheet s, string? createdByName) =>
        new(
            s.StocktakeSheetId,
            s.StocktakeDate,
            s.Note,
            s.Lines.Sum(l => l.Difference),
            s.CreatedByUserId,
            createdByName,
            s.CreatedAt,
            [
                .. s
                    .Lines.OrderBy(l => l.InventoryItem.Name)
                    .ThenBy(l => l.StocktakeSheetLineId)
                    .Select(l => new StocktakeSheetLineDto(
                        l.StocktakeSheetLineId,
                        l.InventoryItemId,
                        l.InventoryItem.Code,
                        l.InventoryItem.Name,
                        l.InventoryItem.Unit,
                        l.BookQuantity,
                        l.CountedQuantity,
                        l.Difference
                    )),
            ]
        );

    public static WasteSheetListItemDto ToListItem(WasteSheet s) =>
        new(
            s.WasteSheetId,
            s.WasteDate,
            s.Reason.ToString(),
            s.Lines.Count,
            s.Lines.Sum(l => LineAmount(l)),
            s.CreatedAt
        );

    public static WasteSheetDto ToDto(WasteSheet s, string? createdByName) =>
        new(
            s.WasteSheetId,
            s.WasteDate,
            s.Reason.ToString(),
            s.Note,
            s.Lines.Sum(l => LineAmount(l)),
            s.CreatedByUserId,
            createdByName,
            s.CreatedAt,
            [
                .. s
                    .Lines.OrderBy(l => l.InventoryItem.Name)
                    .ThenBy(l => l.WasteSheetLineId)
                    .Select(l => new WasteSheetLineDto(
                        l.WasteSheetLineId,
                        l.InventoryItemId,
                        l.InventoryItem.Code,
                        l.InventoryItem.Name,
                        l.InventoryItem.Unit,
                        l.Quantity,
                        l.UnitCost,
                        LineAmount(l)
                    )),
            ]
        );

    public static StockReceiptDto ToDto(StockReceipt r, string? createdByName) =>
        new(
            r.StockReceiptId,
            r.SupplierName,
            r.ReceivedDate,
            r.Note,
            r.Lines.Sum(l => LineAmount(l)),
            r.CreatedByUserId,
            createdByName,
            r.CreatedAt,
            [
                .. r
                    .Lines.OrderBy(l => l.StockReceiptLineId)
                    .Select(l => new StockReceiptLineDto(
                        l.StockReceiptLineId,
                        l.InventoryItemId,
                        l.InventoryItem.Code,
                        l.InventoryItem.Name,
                        l.InventoryItem.Unit,
                        l.Quantity,
                        l.UnitCost,
                        LineAmount(l)
                    )),
            ]
        );
}
