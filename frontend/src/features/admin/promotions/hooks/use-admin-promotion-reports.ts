import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { promotionsAdminApi } from "../api/promotions-admin-api";
import type { PromotionReportListParams, PromotionReportRangeParams } from "../types";

export function useAdminPromotionReports(params: PromotionReportListParams, enabled = true) {
  return useQuery({
    queryKey: queryKeys.adminPromotions.reports(params),
    queryFn: () => promotionsAdminApi.getReports(params),
    enabled,
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminPromotionReport(
  id: number,
  params?: PromotionReportRangeParams,
  enabled = true,
) {
  return useQuery({
    queryKey: queryKeys.adminPromotions.report(id, params),
    queryFn: () => promotionsAdminApi.getReport(id, params),
    enabled: enabled && id > 0,
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}
