import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { roomClosuresApi } from "../api/room-closures-api";
import type {
  AffectedShowtimesParams,
  CreateRoomClosureRequest,
  RoomClosureListParams,
} from "../types";

export function useRoomClosures(params?: RoomClosureListParams) {
  return useQuery({
    queryKey: queryKeys.adminRoomClosures.list(params),
    queryFn: () => roomClosuresApi.getClosures(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useRoomClosure(id: number) {
  return useQuery({
    queryKey: queryKeys.adminRoomClosures.detail(id),
    queryFn: () => roomClosuresApi.getClosure(id),
    enabled: id > 0,
    select: (res) => res.data,
  });
}

export function useClosureAffectedShowtimes(id: number, enabled = true) {
  return useQuery({
    queryKey: queryKeys.adminRoomClosures.affected(id),
    queryFn: () => roomClosuresApi.getClosureAffectedShowtimes(id),
    enabled: id > 0 && enabled,
    select: (res) => res.data,
  });
}

export function useAffectedShowtimesPreview(
  params: AffectedShowtimesParams | null,
) {
  return useQuery({
    queryKey: queryKeys.adminRoomClosures.affectedPreview(params ?? {}),
    queryFn: () => roomClosuresApi.previewAffectedShowtimes(params!),
    enabled: params !== null,
    select: (res) => res.data,
    meta: { skipErrorToast: true },
  });
}

export function useCreateRoomClosure() {
  return useMutation({
    meta: { invalidates: "roomClosureChanged" },
    mutationFn: (body: CreateRoomClosureRequest) =>
      roomClosuresApi.createClosure(body),
  });
}

export function useDeleteRoomClosure() {
  return useMutation({
    meta: { invalidates: "roomClosureChanged" },
    mutationFn: (id: number) => roomClosuresApi.deleteClosure(id),
  });
}
