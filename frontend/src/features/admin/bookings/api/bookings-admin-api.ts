import { apiGet, apiPost, apiPostKeepalive } from "@/api/axios";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AdminBookingListParams,
  AdminBookingListItemDto,
  AdminBookingDetailDto,
  CounterHoldRequest,
  CounterHoldResponse,
  CounterCheckoutRequest,
  CounterCheckoutResponse,
} from "@/features/admin/bookings/types";
import type { CancelOrderResponse } from "@/features/client/orders/types";
import { ENDPOINTS } from "@/api/endpoints";

export const bookingsAdminApi = {
  getBookings: (params: AdminBookingListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminBookingListItemDto>>>(ENDPOINTS.admin.bookings.root, { params }),

  getBookingDetail: (bookingId: number) =>
    apiGet<ApiResponse<AdminBookingDetailDto>>(ENDPOINTS.admin.bookings.byId(bookingId)),

  counterHold: (body: CounterHoldRequest) =>
    apiPost<ApiResponse<CounterHoldResponse>>(ENDPOINTS.admin.bookings.counterHold, body),

  counterCheckout: (bookingId: number, body: CounterCheckoutRequest) =>
    apiPost<ApiResponse<CounterCheckoutResponse>>(ENDPOINTS.admin.bookings.counterCheckout(bookingId), body),

  counterCancel: (bookingId: number) =>
    apiPost<ApiResponse<CancelOrderResponse>>(ENDPOINTS.admin.bookings.counterCancel(bookingId)),

  counterCancelKeepalive: (bookingId: number) =>
    apiPostKeepalive(ENDPOINTS.admin.bookings.counterCancel(bookingId)),
};
