using Cinema.Application.DTOs.Common;

namespace Cinema.Application.DTOs.Inventory;

public record InventoryItemDto(
    int InventoryItemId,
    string Code,
    string Name,
    string Unit,
    decimal QuantityOnHand,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal UnitCost,
    decimal StockValue,
    decimal LowStockThreshold,
    bool IsLowStock,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertInventoryItemRequest(
    string Code,
    string Name,
    string Unit,
    decimal LowStockThreshold,
    bool IsActive = true
);

public record InventoryItemListFilter(string? Search, bool? IsActive, bool? LowStockOnly);

public record DeleteInventoryItemResponse(int InventoryItemId, bool Deleted);

public record ComboRecipeLineDto(
    int InventoryItemId,
    string ItemCode,
    string ItemName,
    string Unit,
    decimal Quantity
);

public record ComboRecipeDto(
    int ComboId,
    string ComboName,
    List<ComboRecipeLineDto> Lines,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ComboRecipeListItemDto(
    int ComboId,
    string ComboName,
    int LineCount,
    DateTime UpdatedAt
);

public record ComboRecipeListFilter(string? Search);

public record ComboRecipeLineRequest(int InventoryItemId, decimal Quantity);

public record UpsertComboRecipeRequest(List<ComboRecipeLineRequest> Lines);

public record DeleteComboRecipeResponse(int ComboId, bool Deleted);

public record StockReceiptLineDto(
    int StockReceiptLineId,
    int InventoryItemId,
    string ItemCode,
    string ItemName,
    string Unit,
    decimal Quantity,
    decimal UnitCost,
    decimal LineAmount
);

public record StockReceiptDto(
    int StockReceiptId,
    string SupplierName,
    DateOnly ReceivedDate,
    string? Note,
    decimal TotalAmount,
    int? CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAt,
    List<StockReceiptLineDto> Lines
);

public record StockReceiptListItemDto(
    int StockReceiptId,
    string SupplierName,
    DateOnly ReceivedDate,
    int LineCount,
    decimal TotalAmount,
    DateTime CreatedAt
);

public record StockReceiptListFilter(
    string? Search,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int? InventoryItemId
);

public record StockReceiptLineRequest(int InventoryItemId, decimal Quantity, decimal UnitCost);

public record CreateStockReceiptRequest(
    string SupplierName,
    DateOnly ReceivedDate,
    string? Note,
    List<StockReceiptLineRequest> Lines
);

public record StocktakeSheetLineRequest(int InventoryItemId, decimal CountedQuantity);

public record CreateStocktakeSheetRequest(
    DateOnly StocktakeDate,
    string? Note,
    List<StocktakeSheetLineRequest> Lines
);

public record StocktakeSheetLineDto(
    int StocktakeSheetLineId,
    int InventoryItemId,
    string ItemCode,
    string ItemName,
    string Unit,
    decimal BookQuantity,
    decimal CountedQuantity,
    decimal Difference
);

public record StocktakeSheetDto(
    int StocktakeSheetId,
    DateOnly StocktakeDate,
    string? Note,
    decimal TotalDifference,
    int? CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAt,
    List<StocktakeSheetLineDto> Lines
);

public record StocktakeSheetListItemDto(
    int StocktakeSheetId,
    DateOnly StocktakeDate,
    string? Note,
    int LineCount,
    decimal TotalDifference,
    DateTime CreatedAt
);

public record StocktakeSheetListFilter(
    string? Search,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int? InventoryItemId
);

public record WasteSheetLineRequest(int InventoryItemId, decimal Quantity);

public record CreateWasteSheetRequest(
    DateOnly WasteDate,
    string Reason,
    string? Note,
    List<WasteSheetLineRequest> Lines
);

public record WasteSheetLineDto(
    int WasteSheetLineId,
    int InventoryItemId,
    string ItemCode,
    string ItemName,
    string Unit,
    decimal Quantity,
    decimal UnitCost,
    decimal LineAmount
);

public record WasteSheetDto(
    int WasteSheetId,
    DateOnly WasteDate,
    string Reason,
    string? Note,
    decimal TotalAmount,
    int? CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAt,
    List<WasteSheetLineDto> Lines
);

public record WasteSheetListItemDto(
    int WasteSheetId,
    DateOnly WasteDate,
    string Reason,
    int LineCount,
    decimal TotalAmount,
    DateTime CreatedAt
);

public record WasteSheetListFilter(
    string? Search,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Reason,
    int? InventoryItemId
);

public record StockLevelDto(
    int InventoryItemId,
    string Code,
    string Name,
    string Unit,
    decimal QuantityOnHand,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal LowStockThreshold,
    bool IsLowStock
);

public record StockLevelFilter(string? Search, bool? LowStockOnly);

public record StockLedgerEntryDto(
    long StockLedgerId,
    DateTime OccurredAt,
    string MovementType,
    decimal QuantityChange,
    decimal BalanceAfter,
    decimal UnitCost,
    string? ReferenceType,
    int? ReferenceId,
    string? Reason,
    int? ActorUserId,
    string? ActorName
);

public record StockLedgerResponse(
    int InventoryItemId,
    string Code,
    string Name,
    string Unit,
    decimal QuantityOnHand,
    decimal OpeningBalance,
    decimal TotalIn,
    decimal TotalOut,
    decimal ClosingBalance,
    OffsetPageResult<StockLedgerEntryDto> Entries
);

public record InventoryValuationItemDto(
    int InventoryItemId,
    string Code,
    string Name,
    string Unit,
    decimal QuantityOnHand,
    decimal UnitCost,
    decimal StockValue
);

public record InventoryValuationDto(
    DateTime GeneratedAt,
    decimal TotalValue,
    List<InventoryValuationItemDto> Items
);
