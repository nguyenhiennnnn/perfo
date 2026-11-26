import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { roomRentalRatesAdminApi } from "../api/room-rentals-admin-api";
import type { RoomRentalRateListParams, UpsertRoomRentalRateRequest } from "../types";

export function useAdminRoomRentalRates(params?: RoomRentalRateListParams) {
  return useQuery({
    queryKey: queryKeys.adminRoomRentalRates.list(params),
    queryFn: () => roomRentalRatesAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminCreateRoomRentalRate() {
  return useMutation({
    meta: { invalidates: "roomRentalRateChanged" },
    mutationFn: (body: UpsertRoomRentalRateRequest) => roomRentalRatesAdminApi.create(body),
  });
}

export function useAdminUpdateRoomRentalRate() {
  return useMutation({
    meta: { invalidates: "roomRentalRateChanged" },
    mutationFn: ({ id, body }: { id: number; body: UpsertRoomRentalRateRequest }) =>
      roomRentalRatesAdminApi.update(id, body),
  });
}

export function useAdminDeleteRoomRentalRate() {
  return useMutation({
    meta: { invalidates: "roomRentalRateChanged" },
    mutationFn: (id: number) => roomRentalRatesAdminApi.delete(id),
  });
}
