import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { loyaltyAdminApi } from "../api/loyalty-admin-api";
import type { LoyaltyChallengeListParams, UpsertLoyaltyChallengeRequest } from "../types";

export function useAdminLoyaltyChallenges(params: LoyaltyChallengeListParams) {
  return useQuery({
    queryKey: queryKeys.adminLoyaltyChallenges.list(params),
    queryFn: () => loyaltyAdminApi.getChallenges(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminCreateLoyaltyChallenge() {
  return useMutation({
    meta: { invalidates: "loyaltyChallengeChanged", skipErrorToast: true },
    mutationFn: (body: UpsertLoyaltyChallengeRequest) => loyaltyAdminApi.createChallenge(body),
  });
}

export function useAdminUpdateLoyaltyChallenge() {
  return useMutation({
    meta: { invalidates: "loyaltyChallengeChanged", skipErrorToast: true },
    mutationFn: ({ id, body }: { id: number; body: UpsertLoyaltyChallengeRequest }) =>
      loyaltyAdminApi.updateChallenge(id, body),
  });
}

export function useAdminDeleteLoyaltyChallenge() {
  return useMutation({
    meta: { invalidates: "loyaltyChallengeChanged" },
    mutationFn: (id: number) => loyaltyAdminApi.deleteChallenge(id),
  });
}
