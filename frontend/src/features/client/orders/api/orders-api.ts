import { apiGet, apiPost } from "@/api/axios";
import type { ApiResponse, CursorPageResult } from "@/types/api";
import type {
  HoldSeatsRequest,
  HoldOrderResponse,
  CheckoutRequest,
  CheckoutResponse,
  CancelOrderRequest,
  CancelOrderResponse,
  OrderListParams,
  OrderListItemDto,
  OrderDetailDto,
} from "../types";
import { ENDPOINTS } from "@/api/endpoints";

export const ordersApi = {
  holdSeats: (body: HoldSeatsRequest) =>
    apiPost<ApiResponse<HoldOrderResponse>>(ENDPOINTS.orders.hold, body),

  getMyOrders: (params: OrderListParams) =>
    apiGet<ApiResponse<CursorPageResult<OrderListItemDto>>>(ENDPOINTS.orders.root, { params }),

  getOrderDetail: (orderId: number) =>
    apiGet<ApiResponse<OrderDetailDto>>(ENDPOINTS.orders.byId(orderId)),

  applyPayment: (orderId: number, body: CheckoutRequest) =>
    apiPost<ApiResponse<CheckoutResponse>>(ENDPOINTS.orders.checkout(orderId), body),

  cancelOrder: (orderId: number, body: CancelOrderRequest) =>
    apiPost<ApiResponse<CancelOrderResponse>>(ENDPOINTS.orders.cancel(orderId), body),
};
