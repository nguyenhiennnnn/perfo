import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { promotionsApi } from "../api/promotions-api";

export function useMyPromotions(enabled = true) {
  return useQuery({
    queryKey: queryKeys.promotions.mine(),
    queryFn: () => promotionsApi.getMyPromotions(),
    enabled,
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}
