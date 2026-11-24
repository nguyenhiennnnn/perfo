import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { counterShiftApi } from "../api/counter-shift-api";
import type {
  CloseCounterShiftRequest,
  OpenCounterShiftRequest,
} from "../types/counter-shift";

const CURRENT_SHIFT_REFETCH_MS = 60_000;

export function useCurrentCounterShift() {
  return useQuery({
    queryKey: queryKeys.adminCounterShifts.current(),
    queryFn: async () => (await counterShiftApi.getCurrent()).data ?? null,
    refetchInterval: CURRENT_SHIFT_REFETCH_MS,
  });
}

export function useOpenCounterShift() {
  return useMutation({
    meta: { invalidates: "counterShiftChanged" },
    mutationFn: (body: OpenCounterShiftRequest) => counterShiftApi.open(body),
  });
}

export function useCloseCounterShift() {
  return useMutation({
    meta: { invalidates: "counterShiftChanged" },
    mutationFn: ({ id, ...body }: CloseCounterShiftRequest & { id: number }) =>
      counterShiftApi.close(id, body),
  });
}
