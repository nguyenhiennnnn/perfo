import { useMutation, useQuery } from "@tanstack/react-query";

import { queryKeys } from "@/api/query-keys";

import { showtimeEventApi } from "../api/showtime-event-api";
import type {
  UpsertShowtimeEarlyAccessRequest,
  UpsertShowtimeEventGiftsRequest,
  UpsertShowtimeEventLimitsRequest,
  UpsertShowtimeEventRequest,
} from "../types";

const detailOptions = { staleTime: 0, refetchOnMount: "always" as const };

export function useShowtimeEvent(showtimeId: number) {
  return useQuery({
    queryKey: queryKeys.adminShowtimes.event(showtimeId),
    queryFn: () => showtimeEventApi.get(showtimeId),
    meta: { skipErrorToast: true },
    select: (res) => res.data,
    ...detailOptions,
  });
}

export function useShowtimeEventLimits(showtimeId: number, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.adminShowtimes.eventLimits(showtimeId),
    queryFn: () => showtimeEventApi.getLimits(showtimeId),
    enabled,
    select: (res) => res.data,
    ...detailOptions,
  });
}

export function useShowtimeEventGifts(showtimeId: number, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.adminShowtimes.eventGifts(showtimeId),
    queryFn: () => showtimeEventApi.getGifts(showtimeId),
    enabled,
    select: (res) => res.data,
    ...detailOptions,
  });
}

export function useShowtimeEventEarlyAccess(showtimeId: number, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.adminShowtimes.eventEarlyAccess(showtimeId),
    queryFn: () => showtimeEventApi.getEarlyAccess(showtimeId),
    enabled,
    select: (res) => res.data,
    ...detailOptions,
  });
}

const eventMutationMeta = { invalidates: "showtimeEventChanged" } as const;

export function useUpsertShowtimeEvent() {
  return useMutation({
    meta: eventMutationMeta,
    mutationFn: ({ showtimeId, body }: { showtimeId: number; body: UpsertShowtimeEventRequest }) =>
      showtimeEventApi.upsert(showtimeId, body),
  });
}

export function useRemoveShowtimeEvent() {
  return useMutation({
    meta: eventMutationMeta,
    mutationFn: (showtimeId: number) => showtimeEventApi.remove(showtimeId),
  });
}

export function useUpsertShowtimeEventLimits() {
  return useMutation({
    meta: eventMutationMeta,
    mutationFn: ({
      showtimeId,
      body,
    }: {
      showtimeId: number;
      body: UpsertShowtimeEventLimitsRequest;
    }) => showtimeEventApi.upsertLimits(showtimeId, body),
  });
}

export function useUpsertShowtimeEventGifts() {
  return useMutation({
    meta: eventMutationMeta,
    mutationFn: ({
      showtimeId,
      body,
    }: {
      showtimeId: number;
      body: UpsertShowtimeEventGiftsRequest;
    }) => showtimeEventApi.upsertGifts(showtimeId, body),
  });
}

export function useUpsertShowtimeEventEarlyAccess() {
  return useMutation({
    meta: eventMutationMeta,
    mutationFn: ({
      showtimeId,
      body,
    }: {
      showtimeId: number;
      body: UpsertShowtimeEarlyAccessRequest;
    }) => showtimeEventApi.upsertEarlyAccess(showtimeId, body),
  });
}

export function useRemoveShowtimeEventEarlyAccess() {
  return useMutation({
    meta: eventMutationMeta,
    mutationFn: (showtimeId: number) => showtimeEventApi.removeEarlyAccess(showtimeId),
  });
}
