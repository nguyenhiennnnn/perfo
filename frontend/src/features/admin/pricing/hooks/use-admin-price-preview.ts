import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { pricingPreviewAdminApi } from "../api/pricing-api";

export function useAdminPricePreview(showtimeId: number | null, at?: string) {
  return useQuery({
    queryKey: queryKeys.adminPricingPreview.table(showtimeId ?? 0, at),
    queryFn: () => pricingPreviewAdminApi.getPriceTable({ showtimeId: showtimeId!, at }),
    enabled: showtimeId !== null,
    select: (res) => res.data,
  });
}
