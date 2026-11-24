import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";

import { queryKeys } from "@/api/query-keys";
import { LOOKUP_PAGE_SIZE } from "@/config/default-constants";

import {
  comboRecipesAdminApi,
  inventoryItemsAdminApi,
  inventoryStockAdminApi,
  unclaimedComboOrdersAdminApi,
} from "../api/inventory-api";
import type {
  ComboRecipeListParams,
  InventoryItemListParams,
  StockLedgerParams,
  StockLevelListParams,
  UnclaimedComboListParams,
  UpsertComboRecipeRequest,
  UpsertInventoryItemRequest,
} from "../types";

export function useAdminInventoryItems(params?: InventoryItemListParams) {
  return useQuery({
    queryKey: queryKeys.adminInventory.items(params),
    queryFn: () => inventoryItemsAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

const ITEM_OPTIONS_PARAMS: InventoryItemListParams = { page: 1, pageSize: LOOKUP_PAGE_SIZE };

export function useAdminInventoryItemOptions() {
  return useQuery({
    queryKey: queryKeys.adminInventory.itemOptions(),
    queryFn: () => inventoryItemsAdminApi.getAll(ITEM_OPTIONS_PARAMS),
    select: (res) => res.data.items,
  });
}

export function useAdminCreateInventoryItem() {
  return useMutation({
    meta: { invalidates: "inventoryChanged" },
    mutationFn: (body: UpsertInventoryItemRequest) => inventoryItemsAdminApi.create(body),
  });
}

export function useAdminUpdateInventoryItem() {
  return useMutation({
    meta: { invalidates: "inventoryChanged" },
    mutationFn: ({ id, body }: { id: number; body: UpsertInventoryItemRequest }) =>
      inventoryItemsAdminApi.update(id, body),
  });
}

export function useAdminDeleteInventoryItem() {
  return useMutation({
    meta: { invalidates: "inventoryChanged" },
    mutationFn: (id: number) => inventoryItemsAdminApi.delete(id),
  });
}

export function useAdminComboRecipes(params?: ComboRecipeListParams) {
  return useQuery({
    queryKey: queryKeys.adminInventory.recipes(params),
    queryFn: () => comboRecipesAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminComboRecipe(comboId: number | null) {
  return useQuery({
    queryKey: queryKeys.adminInventory.recipe(comboId ?? 0),
    queryFn: () => comboRecipesAdminApi.getByComboId(comboId as number),
    enabled: comboId !== null,
    select: (res) => res.data,
  });
}

export function useAdminUpsertComboRecipe() {
  return useMutation({
    meta: { invalidates: "inventoryChanged" },
    mutationFn: ({ comboId, body }: { comboId: number; body: UpsertComboRecipeRequest }) =>
      comboRecipesAdminApi.upsert(comboId, body),
  });
}

export function useAdminDeleteComboRecipe() {
  return useMutation({
    meta: { invalidates: "inventoryChanged" },
    mutationFn: (comboId: number) => comboRecipesAdminApi.delete(comboId),
  });
}

export function useAdminStockLevels(params?: StockLevelListParams) {
  return useQuery({
    queryKey: queryKeys.adminInventory.stock(params),
    queryFn: () => inventoryStockAdminApi.getStock(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminStockAlerts() {
  return useQuery({
    queryKey: queryKeys.adminInventory.alerts(),
    queryFn: () => inventoryStockAdminApi.getAlerts(),
    select: (res) => res.data,
  });
}

export function useAdminStockLedger(params: Partial<StockLedgerParams> & { enabled: boolean }) {
  const { enabled, ...query } = params;
  return useQuery({
    queryKey: queryKeys.adminInventory.ledger(query),
    queryFn: () => inventoryStockAdminApi.getLedger(query as StockLedgerParams),
    enabled: enabled && query.inventoryItemId !== undefined,
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminInventoryValuation() {
  return useQuery({
    queryKey: queryKeys.adminInventory.valuation(),
    queryFn: () => inventoryStockAdminApi.getValuation(),
    select: (res) => res.data,
  });
}

export function useAdminUnclaimedComboOrders(params?: UnclaimedComboListParams) {
  return useQuery({
    queryKey: queryKeys.adminComboPickups.unclaimed(params),
    queryFn: () => unclaimedComboOrdersAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}
