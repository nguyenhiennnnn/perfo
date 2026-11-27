import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { membershipPlansAdminApi } from "../api/membership-plans-admin-api";
import type {
  MembershipPlanListParams,
  MembershipSubscriptionListParams,
  RefundMembershipSubscriptionRequest,
  UpsertMembershipPlanRequest,
} from "../types";

export function useAdminMembershipPlans(params: MembershipPlanListParams) {
  return useQuery({
    queryKey: queryKeys.adminMembershipPlans.list(params),
    queryFn: () => membershipPlansAdminApi.getPlans(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminMembershipPlanLookup() {
  return useQuery({
    queryKey: queryKeys.adminMembershipPlans.lookup(),
    queryFn: () => membershipPlansAdminApi.getPlans({ page: 1, pageSize: 100 }),
    select: (res) => res.data.items,
  });
}

export function useAdminCreateMembershipPlan() {
  return useMutation({
    meta: { invalidates: "membershipPlanChanged", skipErrorToast: true },
    mutationFn: (body: UpsertMembershipPlanRequest) => membershipPlansAdminApi.createPlan(body),
  });
}

export function useAdminUpdateMembershipPlan() {
  return useMutation({
    meta: { invalidates: "membershipPlanChanged", skipErrorToast: true },
    mutationFn: ({ id, body }: { id: number; body: UpsertMembershipPlanRequest }) =>
      membershipPlansAdminApi.updatePlan(id, body),
  });
}

export function useAdminDeleteMembershipPlan() {
  return useMutation({
    meta: { invalidates: "membershipPlanChanged" },
    mutationFn: (id: number) => membershipPlansAdminApi.deletePlan(id),
  });
}

export function useAdminMembershipSubscriptions(params: MembershipSubscriptionListParams) {
  return useQuery({
    queryKey: queryKeys.adminMembershipSubscriptions.list(params),
    queryFn: () => membershipPlansAdminApi.getSubscriptions(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminRefundMembershipSubscription() {
  return useMutation({
    meta: { invalidates: "membershipSubscriptionRefunded", skipErrorToast: true },
    mutationFn: ({ id, body }: { id: number; body: RefundMembershipSubscriptionRequest }) =>
      membershipPlansAdminApi.refundSubscription(id, body),
  });
}
