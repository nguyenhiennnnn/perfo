import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { specialDaysAdminApi } from "../api/pricing-api";
import type { SpecialDayListParams, UpsertSpecialDayRequest } from "../types";

export function useAdminSpecialDays(params?: SpecialDayListParams) {
  return useQuery({
    queryKey: queryKeys.adminSpecialDays.list(params),
    queryFn: () => specialDaysAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminCreateSpecialDay() {
  return useMutation({
    meta: { invalidates: "specialDayChanged" },
    mutationFn: (body: UpsertSpecialDayRequest) => specialDaysAdminApi.create(body),
  });
}

export function useAdminUpdateSpecialDay() {
  return useMutation({
    meta: { invalidates: "specialDayChanged" },
    mutationFn: ({ id, body }: { id: number; body: UpsertSpecialDayRequest }) =>
      specialDaysAdminApi.update(id, body),
  });
}

export function useAdminDeleteSpecialDay() {
  return useMutation({
    meta: { invalidates: "specialDayChanged" },
    mutationFn: (id: number) => specialDaysAdminApi.delete(id),
  });
}
