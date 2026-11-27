import { useMemo } from "react";
import { getServerNow } from "@/lib/server-clock";
import { useMembershipPlans, useMySubscriptions } from "./use-membership-plans";
import { groupSubscriptions } from "../utils/membership-plans";

export function useActiveMembership(enabled = true) {
  const subscriptionsQuery = useMySubscriptions(enabled);
  const plansQuery = useMembershipPlans();

  const subscription = useMemo(
    () =>
      subscriptionsQuery.data
        ? groupSubscriptions(subscriptionsQuery.data, getServerNow()).current
        : null,
    [subscriptionsQuery.data],
  );
  const plan = subscription
    ? plansQuery.data?.find((item) => item.membershipPlanId === subscription.membershipPlanId)
    : undefined;

  return {
    subscription,
    plan,
    isLoading: enabled && (subscriptionsQuery.isLoading || (subscription !== null && plansQuery.isLoading)),
  };
}
