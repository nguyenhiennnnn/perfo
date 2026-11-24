import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { roomChangeRefundsApi } from "../api/room-change-refunds-api";
import type { RoomChangeRefundListParams } from "../types";

export function useRoomChangeRefunds(params: RoomChangeRefundListParams) {
  return useQuery({
    queryKey: queryKeys.adminRoomChangeRefunds.list(params),
    queryFn: () => roomChangeRefundsApi.getRoomChangeRefunds(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useConfirmRoomChangeRefund() {
  return useMutation({
    meta: { invalidates: "bookingChanged", skipSuccessToast: true },
    mutationFn: (roomChangeRefundDueId: number) =>
      roomChangeRefundsApi.confirmRoomChangeRefund(roomChangeRefundDueId),
  });
}
