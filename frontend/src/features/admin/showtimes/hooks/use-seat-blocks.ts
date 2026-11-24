import { useMutation, useQuery } from "@tanstack/react-query";

import { queryKeys } from "@/api/query-keys";
import { showtimesApi } from "@/features/client/showtimes/api/showtimes-api";

import { seatBlocksApi } from "../api/seat-blocks-api";
import type { BlockSeatsRequest, UnblockSeatsRequest } from "../types";

export function useManagedShowtimeSeats(showtimeId: number) {
  return useQuery({
    queryKey: queryKeys.showtimes.seats(showtimeId),
    queryFn: () => showtimesApi.getSeats(showtimeId),
    enabled: showtimeId > 0,
    staleTime: 0,
    refetchOnMount: "always",
    select: (res) => res.data,
  });
}

export function useSeatBlocks(showtimeId: number) {
  return useQuery({
    queryKey: queryKeys.adminShowtimes.seatBlocks(showtimeId),
    queryFn: () => seatBlocksApi.getSeatBlocks(showtimeId),
    enabled: showtimeId > 0,
    staleTime: 0,
    refetchOnMount: "always",
    select: (res) => res.data,
  });
}

export function useBlockSeats() {
  return useMutation({
    meta: { invalidates: "seatBlocksChanged", skipErrorToast: true },
    mutationFn: ({
      showtimeId,
      body,
    }: {
      showtimeId: number;
      body: BlockSeatsRequest;
    }) => seatBlocksApi.blockSeats(showtimeId, body),
  });
}

export function useUnblockSeats() {
  return useMutation({
    meta: { invalidates: "seatBlocksChanged", skipErrorToast: true },
    mutationFn: ({
      showtimeId,
      body,
    }: {
      showtimeId: number;
      body: UnblockSeatsRequest;
    }) => seatBlocksApi.unblockSeats(showtimeId, body),
  });
}
