import { keepPreviousData, useMutation, useQueries, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { saveBlob } from "@/lib/download";
import type { RoomRentalRequestStatus } from "@/types/enum";
import { roomRentalsAdminApi } from "../api/room-rentals-admin-api";
import type {
  ChangeRoomRentalStatusBody,
  RoomRentalListParams,
  RoomRentalQuotePricingRequest,
  UpsertRoomRentalQuoteVersionBody,
} from "../types";

export function useAdminRoomRentals(params: RoomRentalListParams) {
  return useQuery({
    queryKey: queryKeys.adminRoomRentals.list(params),
    queryFn: () => roomRentalsAdminApi.getList(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminRoomRentalBoard(
  statuses: RoomRentalRequestStatus[],
  search: string | undefined,
  pageSize: number,
) {
  return useQueries({
    queries: statuses.map((status) => {
      const params: RoomRentalListParams = { status, search, page: 1, pageSize };
      return {
        queryKey: queryKeys.adminRoomRentals.list(params),
        queryFn: () => roomRentalsAdminApi.getList(params),
        placeholderData: keepPreviousData,
        select: (res: Awaited<ReturnType<typeof roomRentalsAdminApi.getList>>) => res.data,
      };
    }),
  });
}

export function useAdminRoomRental(id: number) {
  return useQuery({
    queryKey: queryKeys.adminRoomRentals.detail(id),
    queryFn: () => roomRentalsAdminApi.getById(id),
    enabled: id > 0,
    select: (res) => res.data,
  });
}

export function useChangeRoomRentalStatus(id: number) {
  return useMutation({
    meta: { invalidates: "roomRentalChanged" },
    mutationFn: (body: ChangeRoomRentalStatusBody) => roomRentalsAdminApi.changeStatus(id, body),
  });
}

export function useAdminRoomRentalQuotes(id: number) {
  return useQuery({
    queryKey: queryKeys.adminRoomRentals.quotes(id),
    queryFn: () => roomRentalsAdminApi.getQuotes(id),
    enabled: id > 0,
    select: (res) => res.data,
  });
}

export function useAdminRoomRentalQuote(id: number, version: number | null) {
  return useQuery({
    queryKey: queryKeys.adminRoomRentals.quote(id, version ?? 0),
    queryFn: () => roomRentalsAdminApi.getQuote(id, version ?? 0),
    enabled: id > 0 && version !== null && version > 0,
    select: (res) => res.data,
  });
}

export function useCreateRoomRentalQuote(id: number) {
  return useMutation({
    meta: { invalidates: "roomRentalChanged" },
    mutationFn: (body: UpsertRoomRentalQuoteVersionBody) =>
      roomRentalsAdminApi.createQuote(id, body),
  });
}

export function useSendRoomRentalQuote(id: number) {
  return useMutation({
    meta: { invalidates: "roomRentalChanged" },
    mutationFn: (version: number) => roomRentalsAdminApi.sendQuote(id, version),
  });
}

export function useRoomRentalQuotePreview(request: RoomRentalQuotePricingRequest | null) {
  return useQuery({
    queryKey: queryKeys.adminRoomRentals.calculate(request ?? {}),
    queryFn: () => roomRentalsAdminApi.calculate(request as RoomRentalQuotePricingRequest),
    enabled: request !== null,
    retry: false,
    placeholderData: keepPreviousData,
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}

export function useDownloadAdminRoomRentalQuotePdf(id: number) {
  return useMutation({
    meta: { errorTitle: "Không thể tải báo giá PDF", skipSuccessToast: true },
    mutationFn: async (version: number) => {
      const { blob, filename } = await roomRentalsAdminApi.downloadQuotePdf(
        id,
        version,
        `bao-gia-v${version}.pdf`,
      );
      saveBlob(blob, filename);
    },
  });
}

