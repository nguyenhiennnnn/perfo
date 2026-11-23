import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { promotionsAdminApi } from "../api/promotions-admin-api";
import type { PromotionRedemptionListParams } from "../types";

export function useAdminPromotionRedemptions(
  promotionId: number,
  params: PromotionRedemptionListParams,
) {
  return useQuery({
    queryKey: queryKeys.adminPromotions.redemptions(promotionId, params),
    queryFn: () => promotionsAdminApi.getRedemptions(promotionId, params),
    enabled: promotionId > 0,
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}
