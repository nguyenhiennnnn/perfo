import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import type { RoomRentalRequestStatus } from "@/types/enum";
import { roomRentalsAdminApi } from "../api/room-rentals-admin-api";
import { PAYMENT_VISIBLE_STATUSES } from "../utils/room-rental-admin-labels";

export function useAdminRoomRentalPayments(id: number, status: RoomRentalRequestStatus) {
  return useQuery({
    queryKey: queryKeys.adminRoomRentals.payments(id),
    queryFn: () => roomRentalsAdminApi.getPayments(id),
    enabled: id > 0 && PAYMENT_VISIBLE_STATUSES.includes(status),
    retry: false,
    meta: { skipErrorToast: true },
    select: (res) => res.data,
  });
}
