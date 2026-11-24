import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { counterShiftsAdminApi } from "../api/counter-shifts-admin-api";
import type { AdminCloseCounterShiftRequest, CounterShiftListParams } from "../types";

const OPEN_SHIFTS_REFETCH_MS = 60_000;

export function useAdminCounterShifts(params: CounterShiftListParams) {
  return useQuery({
    queryKey: queryKeys.adminCounterShifts.list(params),
    queryFn: () => counterShiftsAdminApi.getShifts(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminOpenCounterShifts() {
  return useQuery({
    queryKey: queryKeys.adminCounterShifts.open(),
    queryFn: () => counterShiftsAdminApi.getOpenShifts(),
    refetchInterval: OPEN_SHIFTS_REFETCH_MS,
    select: (res) => res.data,
  });
}

export function useAdminCloseCounterShift() {
  return useMutation({
    meta: { invalidates: "counterShiftChanged" },
    mutationFn: ({ id, ...body }: AdminCloseCounterShiftRequest & { id: number }) =>
      counterShiftsAdminApi.closeByAdmin(id, body),
  });
}
