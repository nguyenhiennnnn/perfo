import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { loyaltyAdminApi } from "../api/loyalty-admin-api";
import type { AdjustLoyaltyPointsRequest, LoyaltyTransactionListParams } from "../types";

export function useAdminLoyaltyCustomerSearch(query: string) {
  const keyword = query.trim();
  return useQuery({
    queryKey: queryKeys.adminLoyaltyCustomers.search(keyword),
    queryFn: () => loyaltyAdminApi.searchCustomers(keyword),
    enabled: keyword.length > 0,
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminLoyaltyCustomer(userId: number | null) {
  return useQuery({
    queryKey: queryKeys.adminLoyaltyCustomers.detail(userId ?? 0),
    queryFn: () => loyaltyAdminApi.getCustomer(userId as number),
    enabled: userId !== null,
    select: (res) => res.data,
  });
}

export function useAdminLoyaltyCustomerTransactions(
  userId: number | null,
  params: LoyaltyTransactionListParams,
) {
  return useQuery({
    queryKey: queryKeys.adminLoyaltyCustomers.transactions(userId ?? 0, params),
    queryFn: () => loyaltyAdminApi.getCustomerTransactions(userId as number, params),
    enabled: userId !== null,
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminAdjustLoyaltyPoints() {
  return useMutation({
    meta: { invalidates: "loyaltyPointsAdjusted", skipErrorToast: true },
    mutationFn: ({ userId, body }: { userId: number; body: AdjustLoyaltyPointsRequest }) =>
      loyaltyAdminApi.adjustPoints(userId, body),
  });
}
