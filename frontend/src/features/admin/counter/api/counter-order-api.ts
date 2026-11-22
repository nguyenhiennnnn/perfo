import { apiPost, apiPostKeepalive } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { CancelReason } from "@/types/enum";
import type { CancelOrderResponse } from "@/features/admin/orders/types";
import type {
  CounterCancelRequest,
  CounterCheckoutRequest,
  CounterCheckoutResponse,
  CounterComboOrderRequest,
  CounterOrderResponse,
} from "../types";

export const counterOrderApi = {
  createComboOrder: (body: CounterComboOrderRequest) =>
    apiPost<ApiResponse<CounterOrderResponse>>(
      ENDPOINTS.admin.bookings.counterComboOrder,
      body,
    ),

  checkout: (orderId: number, body: CounterCheckoutRequest) =>
    apiPost<ApiResponse<CounterCheckoutResponse>>(
      ENDPOINTS.admin.bookings.counterCheckout(orderId),
      body,
    ),

  cancel: (orderId: number, reason: CancelReason) =>
    apiPost<ApiResponse<CancelOrderResponse>>(
      ENDPOINTS.admin.bookings.counterCancel(orderId),
      { reason } satisfies CounterCancelRequest,
    ),

  cancelKeepalive: (orderId: number, reason: CancelReason) =>
    apiPostKeepalive(ENDPOINTS.admin.bookings.counterCancel(orderId), {
      reason,
    } satisfies CounterCancelRequest),
};
