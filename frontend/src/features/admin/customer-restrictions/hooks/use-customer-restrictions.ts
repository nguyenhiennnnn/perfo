import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { customerRestrictionsApi } from "../api/customer-restrictions-api";
import type { CustomerRestrictionListParams } from "../types";

export function useCustomerRestrictions(params: CustomerRestrictionListParams) {
  return useQuery({
    queryKey: queryKeys.adminCustomerRestrictions.list(params),
    queryFn: () => customerRestrictionsApi.getRestrictions(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useLiftCustomerRestriction() {
  return useMutation({
    meta: { invalidates: "customerRestrictionChanged" },
    mutationFn: (id: number) => customerRestrictionsApi.liftRestriction(id),
  });
}
