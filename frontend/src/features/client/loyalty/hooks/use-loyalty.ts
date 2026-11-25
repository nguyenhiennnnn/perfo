import { useInfiniteQuery, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { loyaltyApi } from "../api/loyalty-api";
import type { LoyaltyLotListParams } from "../types";

export function useLoyaltySummary() {
  return useQuery({
    queryKey: queryKeys.loyalty.summary(),
    queryFn: () => loyaltyApi.getSummary(),
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function useLoyaltyRewards() {
  return useQuery({
    queryKey: queryKeys.loyalty.rewards(),
    queryFn: () => loyaltyApi.getRewards(),
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function useMyChallenges() {
  return useQuery({
    queryKey: queryKeys.loyalty.challenges(),
    queryFn: () => loyaltyApi.getChallenges(),
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function useMyReferrals() {
  return useQuery({
    queryKey: queryKeys.loyalty.referrals(),
    queryFn: () => loyaltyApi.getReferrals(),
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function useLoyaltyLots(params: Omit<LoyaltyLotListParams, "page">) {
  return useInfiniteQuery({
    queryKey: queryKeys.loyalty.lots(params),
    queryFn: ({ pageParam }) => loyaltyApi.getLots({ ...params, page: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.data.page < lastPage.data.totalPages ? lastPage.data.page + 1 : undefined,
    meta: { skipErrorToast: true },
    select: (data) => ({
      lots: data.pages.flatMap((page) => page.data.items),
      totalItems: data.pages.at(-1)?.data.totalItems ?? 0,
    }),
  });
}

export function useRedeemLoyaltyReward() {
  return useMutation({
    meta: { invalidates: "loyaltyChanged" },
    mutationFn: (rewardId: number) => loyaltyApi.redeemReward(rewardId),
  });
}
