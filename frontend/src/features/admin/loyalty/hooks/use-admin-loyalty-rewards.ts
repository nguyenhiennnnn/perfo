import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { loyaltyAdminApi } from "../api/loyalty-admin-api";
import type { LoyaltyRewardListParams, UpsertLoyaltyRewardRequest } from "../types";

export function useAdminLoyaltyRewards(params: LoyaltyRewardListParams) {
  return useQuery({
    queryKey: queryKeys.adminLoyaltyRewards.list(params),
    queryFn: () => loyaltyAdminApi.getRewards(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminCreateLoyaltyReward() {
  return useMutation({
    meta: { invalidates: "loyaltyRewardChanged", skipErrorToast: true },
    mutationFn: (body: UpsertLoyaltyRewardRequest) => loyaltyAdminApi.createReward(body),
  });
}

export function useAdminUpdateLoyaltyReward() {
  return useMutation({
    meta: { invalidates: "loyaltyRewardChanged", skipErrorToast: true },
    mutationFn: ({ id, body }: { id: number; body: UpsertLoyaltyRewardRequest }) =>
      loyaltyAdminApi.updateReward(id, body),
  });
}

export function useAdminDeleteLoyaltyReward() {
  return useMutation({
    meta: { invalidates: "loyaltyRewardChanged" },
    mutationFn: (id: number) => loyaltyAdminApi.deleteReward(id),
  });
}
