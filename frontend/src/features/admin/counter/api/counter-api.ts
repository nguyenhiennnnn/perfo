import { apiPost, apiPostKeepalive } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { CancelOrderRequest, CancelOrderResponse } from "@/features/admin/orders/types";
import type {
  CounterCheckoutRequest,
  CounterCheckoutResponse,
  CounterComboOrderRequest,
  CounterHoldRequest,
  CounterOrderResponse,
} from "../types";

export const counterApi = {
  holdOrder: (body: CounterHoldRequest) =>
    apiPost<ApiResponse<CounterOrderResponse>>(ENDPOINTS.admin.bookings.counterHold, body),

  createComboOrder: (body: CounterComboOrderRequest) =>
    apiPost<ApiResponse<CounterOrderResponse>>(ENDPOINTS.admin.bookings.counterComboOrder, body),

  checkout: (orderId: number, body: CounterCheckoutRequest) =>
    apiPost<ApiResponse<CounterCheckoutResponse>>(
      ENDPOINTS.admin.bookings.counterCheckout(orderId),
      body,
    ),

  cancelOrder: (orderId: number, body: CancelOrderRequest) =>
    apiPost<ApiResponse<CancelOrderResponse>>(
      ENDPOINTS.admin.bookings.counterCancel(orderId),
      body,
    ),

  cancelOrderKeepalive: (orderId: number, body: CancelOrderRequest) =>
    apiPostKeepalive(ENDPOINTS.admin.bookings.counterCancel(orderId), body),
};
