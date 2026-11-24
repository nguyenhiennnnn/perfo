import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";

import { queryKeys } from "@/api/query-keys";

import { stockReceiptsAdminApi, stocktakesAdminApi, wasteAdminApi } from "../api/inventory-api";
import type {
  CreateStockReceiptRequest,
  CreateStocktakeSheetRequest,
  CreateWasteSheetRequest,
  StockSheetListParams,
} from "../types";

export function useAdminStockReceipts(params?: StockSheetListParams) {
  return useQuery({
    queryKey: queryKeys.adminInventory.receipts(params),
    queryFn: () => stockReceiptsAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminStockReceipt(id: number) {
  return useQuery({
    queryKey: queryKeys.adminInventory.receipt(id),
    queryFn: () => stockReceiptsAdminApi.getById(id),
    select: (res) => res.data,
  });
}

export function useAdminCreateStockReceipt() {
  return useMutation({
    meta: { invalidates: "inventoryChanged" },
    mutationFn: (body: CreateStockReceiptRequest) => stockReceiptsAdminApi.create(body),
  });
}

export function useAdminStocktakes(params?: StockSheetListParams) {
  return useQuery({
    queryKey: queryKeys.adminInventory.stocktakes(params),
    queryFn: () => stocktakesAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminStocktake(id: number) {
  return useQuery({
    queryKey: queryKeys.adminInventory.stocktake(id),
    queryFn: () => stocktakesAdminApi.getById(id),
    select: (res) => res.data,
  });
}

export function useAdminCreateStocktake() {
  return useMutation({
    meta: { invalidates: "inventoryChanged" },
    mutationFn: (body: CreateStocktakeSheetRequest) => stocktakesAdminApi.create(body),
  });
}

export function useAdminWasteSheets(params?: StockSheetListParams) {
  return useQuery({
    queryKey: queryKeys.adminInventory.waste(params),
    queryFn: () => wasteAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminWasteSheet(id: number) {
  return useQuery({
    queryKey: queryKeys.adminInventory.wasteSheet(id),
    queryFn: () => wasteAdminApi.getById(id),
    select: (res) => res.data,
  });
}

export function useAdminCreateWasteSheet() {
  return useMutation({
    meta: { invalidates: "inventoryChanged" },
    mutationFn: (body: CreateWasteSheetRequest) => wasteAdminApi.create(body),
  });
}
