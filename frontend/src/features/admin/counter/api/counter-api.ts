import { apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type {
  CounterCheckoutRequest,
  CounterCheckoutResponse,
  CounterComboOrderRequest,
  CounterHoldRequest,
  CounterOrderResponse,
} from "../types";

export const counterApi = {
  holdOrder: (body: CounterHoldRequest) =>
    apiPost<ApiResponse<CounterOrderResponse>>(ENDPOINTS.admin.orders.counterHold, body),

  createComboOrder: (body: CounterComboOrderRequest) =>
    apiPost<ApiResponse<CounterOrderResponse>>(ENDPOINTS.admin.orders.counterComboOrder, body),

  checkout: (orderId: number, body: CounterCheckoutRequest) =>
    apiPost<ApiResponse<CounterCheckoutResponse>>(
      ENDPOINTS.admin.orders.counterCheckout(orderId),
      body,
    ),
};
