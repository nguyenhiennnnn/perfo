import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { getBusinessToday } from "@/lib/business-time";
import { LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { promotionsAdminApi } from "../api/promotions-admin-api";
import type {
  BulkPersonalCodesRequest,
  PromotionListParams,
  UpsertPromotionRequest,
} from "../types";
import type { PromotionConsumption } from "../utils/promotion-admin-labels";

const USAGE_MAX_PAGES = 10;

export function useAdminPromotions(params: PromotionListParams) {
  return useQuery({
    queryKey: queryKeys.adminPromotions.list(params),
    queryFn: () => promotionsAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminPromotionDetail(id: number) {
  return useQuery({
    queryKey: queryKeys.adminPromotions.detail(id),
    queryFn: () => promotionsAdminApi.getById(id),
    enabled: id > 0,
    select: (res) => res.data,
  });
}

type UsageFilter = Pick<PromotionListParams, "search" | "type" | "isActive">;

export function useAdminPromotionUsageMap(filter: UsageFilter) {
  return useQuery({
    queryKey: queryKeys.adminPromotions.usage(filter),
    queryFn: async () => {
      const today = getBusinessToday();
      const map: Record<number, PromotionConsumption> = {};
      for (let page = 1; page <= USAGE_MAX_PAGES; page += 1) {
        const res = await promotionsAdminApi.getReports({
          ...filter,
          fromDate: today,
          toDate: today,
          page,
          pageSize: LOOKUP_PAGE_SIZE,
        });
        const { items, totalPages } = res.data.items;
        items.forEach((item) => {
          map[item.promotionId] = {
            consumedCount: item.consumedCount,
            consumedBudget: item.consumedBudget,
          };
        });
        if (page >= totalPages) break;
      }
      return map;
    },
    placeholderData: keepPreviousData,
    meta: { skipErrorToast: true },
  });
}

export function useAdminCreatePromotion() {
  return useMutation({
    meta: { invalidates: "promotionChanged", skipErrorToast: true },
    mutationFn: (body: UpsertPromotionRequest) => promotionsAdminApi.create(body),
  });
}

export function useAdminUpdatePromotion() {
  return useMutation({
    meta: { invalidates: "promotionChanged", skipErrorToast: true },
    mutationFn: ({ id, body }: { id: number; body: UpsertPromotionRequest }) =>
      promotionsAdminApi.update(id, body),
  });
}

export function useAdminPausePromotion() {
  return useMutation({
    meta: { invalidates: "promotionChanged" },
    mutationFn: (id: number) => promotionsAdminApi.pause(id),
  });
}

export function useAdminResumePromotion() {
  return useMutation({
    meta: { invalidates: "promotionChanged" },
    mutationFn: (id: number) => promotionsAdminApi.resume(id),
  });
}

export function useAdminDeletePromotion() {
  return useMutation({
    meta: { invalidates: "promotionChanged" },
    mutationFn: (id: number) => promotionsAdminApi.delete(id),
  });
}

export function useAdminCreatePersonalCodes() {
  return useMutation({
    meta: { invalidates: "promotionChanged", skipErrorToast: true },
    mutationFn: (body: BulkPersonalCodesRequest) =>
      promotionsAdminApi.createPersonalCodes(body),
  });
}
