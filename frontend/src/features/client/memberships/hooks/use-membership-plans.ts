import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { membershipPlansApi } from "../api/membership-plans-api";

export function useMembershipPlans() {
  return useQuery({
    queryKey: queryKeys.membership.plans(),
    queryFn: () => membershipPlansApi.getPlans(),
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function useMySubscriptions(enabled = true) {
  return useQuery({
    queryKey: queryKeys.membership.subscriptions(),
    queryFn: () => membershipPlansApi.getMySubscriptions(),
    enabled,
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function useMyFreeTickets(enabled = true) {
  return useQuery({
    queryKey: queryKeys.membership.freeTickets(),
    queryFn: () => membershipPlansApi.getMyFreeTickets(),
    enabled,
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function usePurchaseMembershipPlan() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (planId: number) => membershipPlansApi.purchase(planId),
  });
}
