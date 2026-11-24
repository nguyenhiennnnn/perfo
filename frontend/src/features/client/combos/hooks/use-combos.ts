import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { combosApi } from "../api/combos-api";
import { queryKeys } from "@/api/query-keys";

interface UseActiveCombosOptions {
  alwaysRefetchOnMount?: boolean;
}

export function useActiveCombos(options?: UseActiveCombosOptions) {
  return useQuery({
    queryKey: queryKeys.combos.active(),
    queryFn: () => combosApi.getActiveCombos(),
    ...(options?.alwaysRefetchOnMount && { refetchOnMount: "always" as const }),
    select: (res) => res.data,
  });
}

export function useComboSuggestions(showtimeId: number, seatCount: number) {
  return useQuery({
    queryKey: queryKeys.combos.suggestions(showtimeId, seatCount),
    queryFn: () => combosApi.getSuggestions(showtimeId, seatCount),
    enabled: showtimeId > 0 && seatCount > 0,
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}
