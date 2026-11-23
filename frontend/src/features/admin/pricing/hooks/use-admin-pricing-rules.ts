import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { pricingRulesAdminApi } from "../api/pricing-api";
import type { PricingRuleListParams, UpsertPricingRuleRequest } from "../types";

export function useAdminPricingRules(params?: PricingRuleListParams) {
  return useQuery({
    queryKey: queryKeys.adminPricingRules.list(params),
    queryFn: () => pricingRulesAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminCreatePricingRule() {
  return useMutation({
    meta: { invalidates: "pricingRuleChanged" },
    mutationFn: (body: UpsertPricingRuleRequest) => pricingRulesAdminApi.create(body),
  });
}

export function useAdminUpdatePricingRule() {
  return useMutation({
    meta: { invalidates: "pricingRuleChanged" },
    mutationFn: ({ id, body }: { id: number; body: UpsertPricingRuleRequest }) =>
      pricingRulesAdminApi.update(id, body),
  });
}

export function useAdminDeletePricingRule() {
  return useMutation({
    meta: { invalidates: "pricingRuleChanged" },
    mutationFn: (id: number) => pricingRulesAdminApi.delete(id),
  });
}
