import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { ticketTypesAdminApi } from "../api/pricing-api";
import type { TicketTypeListParams, UpsertTicketTypeRequest } from "../types";

export function useAdminTicketTypes(params?: TicketTypeListParams) {
  return useQuery({
    queryKey: queryKeys.adminTicketTypes.list(params),
    queryFn: () => ticketTypesAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminCreateTicketType() {
  return useMutation({
    meta: { invalidates: "ticketTypeChanged" },
    mutationFn: (body: UpsertTicketTypeRequest) => ticketTypesAdminApi.create(body),
  });
}

export function useAdminUpdateTicketType() {
  return useMutation({
    meta: { invalidates: "ticketTypeChanged" },
    mutationFn: ({ id, body }: { id: number; body: UpsertTicketTypeRequest }) =>
      ticketTypesAdminApi.update(id, body),
  });
}

export function useAdminDeleteTicketType() {
  return useMutation({
    meta: { invalidates: "ticketTypeChanged" },
    mutationFn: (id: number) => ticketTypesAdminApi.delete(id),
  });
}
