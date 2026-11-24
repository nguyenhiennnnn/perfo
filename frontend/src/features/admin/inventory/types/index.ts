import type { OffsetPageResult } from "@/types/api";

export const StockMovementType = {
  Receipt: "Receipt",
  Sale: "Sale",
  Return: "Return",
  StocktakeAdjustment: "StocktakeAdjustment",
  Waste: "Waste",
} as const;
export type StockMovementType = (typeof StockMovementType)[keyof typeof StockMovementType];

export const WasteReason = {
  Expired: "Expired",
  Damaged: "Damaged",
  Spilled: "Spilled",
  Other: "Other",
} as const;
export type WasteReason = (typeof WasteReason)[keyof typeof WasteReason];

export interface InventoryItemDto {
  inventoryItemId: number;
  code: string;
  name: string;
  unit: string;
  quantityOnHand: number;
  reservedQuantity: number;
  availableQuantity: number;
  unitCost: number;
  stockValue: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UpsertInventoryItemRequest {
  code: string;
  name: string;
  unit: string;
  lowStockThreshold: number;
  isActive: boolean;
}

export interface InventoryItemListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
  lowStockOnly?: boolean;
}

export interface DeleteInventoryItemResponse {
  inventoryItemId: number;
  deleted: boolean;
}

export interface ComboRecipeLineDto {
  inventoryItemId: number;
  itemCode: string;
  itemName: string;
  unit: string;
  quantity: number;
}

export interface ComboRecipeDto {
  comboId: number;
  comboName: string;
  lines: ComboRecipeLineDto[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface ComboRecipeListItemDto {
  comboId: number;
  comboName: string;
  lineCount: number;
  updatedAt: string;
}

export interface ComboRecipeListParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

export interface ComboRecipeLineRequest {
  inventoryItemId: number;
  quantity: number;
}

export interface UpsertComboRecipeRequest {
  lines: ComboRecipeLineRequest[];
}

export interface DeleteComboRecipeResponse {
  comboId: number;
  deleted: boolean;
}

export interface StockSheetListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  fromDate?: string;
  toDate?: string;
  inventoryItemId?: number;
  reason?: string;
}

export interface StockReceiptLineDto {
  stockReceiptLineId: number;
  inventoryItemId: number;
  itemCode: string;
  itemName: string;
  unit: string;
  quantity: number;
  unitCost: number;
  lineAmount: number;
}

export interface StockReceiptDto {
  stockReceiptId: number;
  supplierName: string;
  receivedDate: string;
  note?: string | null;
  totalAmount: number;
  createdByUserId?: number | null;
  createdByName?: string | null;
  createdAt: string;
  lines: StockReceiptLineDto[];
}

export interface StockReceiptListItemDto {
  stockReceiptId: number;
  supplierName: string;
  receivedDate: string;
  lineCount: number;
  totalAmount: number;
  createdAt: string;
}

export interface StockReceiptLineRequest {
  inventoryItemId: number;
  quantity: number;
  unitCost: number;
}

export interface CreateStockReceiptRequest {
  supplierName: string;
  receivedDate: string;
  note?: string | null;
  lines: StockReceiptLineRequest[];
}

export interface StocktakeSheetLineDto {
  stocktakeSheetLineId: number;
  inventoryItemId: number;
  itemCode: string;
  itemName: string;
  unit: string;
  bookQuantity: number;
  countedQuantity: number;
  difference: number;
}

export interface StocktakeSheetDto {
  stocktakeSheetId: number;
  stocktakeDate: string;
  note?: string | null;
  totalDifference: number;
  createdByUserId?: number | null;
  createdByName?: string | null;
  createdAt: string;
  lines: StocktakeSheetLineDto[];
}

export interface StocktakeSheetListItemDto {
  stocktakeSheetId: number;
  stocktakeDate: string;
  note?: string | null;
  lineCount: number;
  totalDifference: number;
  createdAt: string;
}

export interface StocktakeSheetLineRequest {
  inventoryItemId: number;
  countedQuantity: number;
}

export interface CreateStocktakeSheetRequest {
  stocktakeDate: string;
  note?: string | null;
  lines: StocktakeSheetLineRequest[];
}

export interface WasteSheetLineDto {
  wasteSheetLineId: number;
  inventoryItemId: number;
  itemCode: string;
  itemName: string;
  unit: string;
  quantity: number;
  unitCost: number;
  lineAmount: number;
}

export interface WasteSheetDto {
  wasteSheetId: number;
  wasteDate: string;
  reason: string;
  note?: string | null;
  totalAmount: number;
  createdByUserId?: number | null;
  createdByName?: string | null;
  createdAt: string;
  lines: WasteSheetLineDto[];
}

export interface WasteSheetListItemDto {
  wasteSheetId: number;
  wasteDate: string;
  reason: string;
  lineCount: number;
  totalAmount: number;
  createdAt: string;
}

export interface WasteSheetLineRequest {
  inventoryItemId: number;
  quantity: number;
}

export interface CreateWasteSheetRequest {
  wasteDate: string;
  reason: string;
  note?: string | null;
  lines: WasteSheetLineRequest[];
}

export interface StockLevelDto {
  inventoryItemId: number;
  code: string;
  name: string;
  unit: string;
  quantityOnHand: number;
  reservedQuantity: number;
  availableQuantity: number;
  lowStockThreshold: number;
  isLowStock: boolean;
}

export interface StockLevelListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  lowStockOnly?: boolean;
}

export interface LowStockAlertDto {
  inventoryItemId: number;
  code: string;
  name: string;
  unit: string;
  quantityOnHand: number;
  reservedQuantity: number;
  availableQuantity: number;
  lowStockThreshold: number;
  isOutOfStock: boolean;
  alertedAt?: string | null;
  affectedCombos: string[];
}

export interface LowStockAlertsResponse {
  count: number;
  items: LowStockAlertDto[];
}

export interface StockLedgerEntryDto {
  stockLedgerId: number;
  occurredAt: string;
  movementType: string;
  quantityChange: number;
  balanceAfter: number;
  unitCost: number;
  referenceType?: string | null;
  referenceId?: number | null;
  reason?: string | null;
  actorUserId?: number | null;
  actorName?: string | null;
}

export interface StockLedgerResponse {
  inventoryItemId: number;
  code: string;
  name: string;
  unit: string;
  quantityOnHand: number;
  openingBalance: number;
  totalIn: number;
  totalOut: number;
  closingBalance: number;
  entries: OffsetPageResult<StockLedgerEntryDto>;
}

export interface StockLedgerParams {
  inventoryItemId: number;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface InventoryValuationItemDto {
  inventoryItemId: number;
  code: string;
  name: string;
  unit: string;
  quantityOnHand: number;
  unitCost: number;
  stockValue: number;
}

export interface InventoryValuationDto {
  generatedAt: string;
  totalValue: number;
  items: InventoryValuationItemDto[];
}

export interface UnclaimedComboLineDto {
  comboName?: string | null;
  quantity: number;
  remainingQuantity: number;
}

export interface UnclaimedComboOrderDto {
  orderId: number;
  orderCode: string;
  customerName?: string | null;
  customerPhone?: string | null;
  createdAt: string;
  saleDate: string;
  daysOverdue: number;
  finalAmount: number;
  totalQuantity: number;
  fulfilledQuantity: number;
  remainingQuantity: number;
  lines: UnclaimedComboLineDto[];
}

export interface UnclaimedComboListParams {
  page?: number;
  pageSize?: number;
  search?: string;
}
