import { apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  ComboRecipeDto,
  ComboRecipeListItemDto,
  ComboRecipeListParams,
  CreateStockReceiptRequest,
  CreateStocktakeSheetRequest,
  CreateWasteSheetRequest,
  DeleteComboRecipeResponse,
  DeleteInventoryItemResponse,
  InventoryItemDto,
  InventoryItemListParams,
  InventoryValuationDto,
  LowStockAlertsResponse,
  StockLedgerParams,
  StockLedgerResponse,
  StockLevelDto,
  StockLevelListParams,
  StockReceiptDto,
  StockReceiptListItemDto,
  StockSheetListParams,
  StocktakeSheetDto,
  StocktakeSheetListItemDto,
  UnclaimedComboListParams,
  UnclaimedComboOrderDto,
  UpsertComboRecipeRequest,
  UpsertInventoryItemRequest,
  WasteSheetDto,
  WasteSheetListItemDto,
} from "../types";

const inventory = ENDPOINTS.admin.inventory;

export const inventoryItemsAdminApi = {
  getAll: (params?: InventoryItemListParams) =>
    apiGet<ApiResponse<OffsetPageResult<InventoryItemDto>>>(inventory.items, { params }),
  create: (body: UpsertInventoryItemRequest) =>
    apiPost<ApiResponse<InventoryItemDto>>(inventory.items, body),
  update: (id: number, body: UpsertInventoryItemRequest) =>
    apiPut<ApiResponse<InventoryItemDto>>(inventory.itemById(id), body),
  delete: (id: number) =>
    apiDelete<ApiResponse<DeleteInventoryItemResponse>>(inventory.itemById(id)),
};

export const comboRecipesAdminApi = {
  getAll: (params?: ComboRecipeListParams) =>
    apiGet<ApiResponse<OffsetPageResult<ComboRecipeListItemDto>>>(inventory.recipes, { params }),
  getByComboId: (comboId: number) =>
    apiGet<ApiResponse<ComboRecipeDto>>(inventory.recipeByComboId(comboId)),
  upsert: (comboId: number, body: UpsertComboRecipeRequest) =>
    apiPut<ApiResponse<ComboRecipeDto>>(inventory.recipeByComboId(comboId), body),
  delete: (comboId: number) =>
    apiDelete<ApiResponse<DeleteComboRecipeResponse>>(inventory.recipeByComboId(comboId)),
};

export const stockReceiptsAdminApi = {
  getAll: (params?: StockSheetListParams) =>
    apiGet<ApiResponse<OffsetPageResult<StockReceiptListItemDto>>>(inventory.receipts, { params }),
  getById: (id: number) => apiGet<ApiResponse<StockReceiptDto>>(inventory.receiptById(id)),
  create: (body: CreateStockReceiptRequest) =>
    apiPost<ApiResponse<StockReceiptDto>>(inventory.receipts, body),
};

export const stocktakesAdminApi = {
  getAll: (params?: StockSheetListParams) =>
    apiGet<ApiResponse<OffsetPageResult<StocktakeSheetListItemDto>>>(inventory.stocktakes, {
      params,
    }),
  getById: (id: number) => apiGet<ApiResponse<StocktakeSheetDto>>(inventory.stocktakeById(id)),
  create: (body: CreateStocktakeSheetRequest) =>
    apiPost<ApiResponse<StocktakeSheetDto>>(inventory.stocktakes, body),
};

export const wasteAdminApi = {
  getAll: (params?: StockSheetListParams) =>
    apiGet<ApiResponse<OffsetPageResult<WasteSheetListItemDto>>>(inventory.waste, { params }),
  getById: (id: number) => apiGet<ApiResponse<WasteSheetDto>>(inventory.wasteById(id)),
  create: (body: CreateWasteSheetRequest) =>
    apiPost<ApiResponse<WasteSheetDto>>(inventory.waste, body),
};

export const inventoryStockAdminApi = {
  getStock: (params?: StockLevelListParams) =>
    apiGet<ApiResponse<OffsetPageResult<StockLevelDto>>>(inventory.stock, { params }),
  getAlerts: () => apiGet<ApiResponse<LowStockAlertsResponse>>(inventory.alerts),
  getLedger: (params: StockLedgerParams) =>
    apiGet<ApiResponse<StockLedgerResponse>>(inventory.ledger, { params }),
  getValuation: () => apiGet<ApiResponse<InventoryValuationDto>>(inventory.valuation),
};

export const unclaimedComboOrdersAdminApi = {
  getAll: (params?: UnclaimedComboListParams) =>
    apiGet<ApiResponse<OffsetPageResult<UnclaimedComboOrderDto>>>(
      ENDPOINTS.admin.comboPickups.unclaimed,
      { params },
    ),
};
