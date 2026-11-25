import { apiDelete, apiGet, apiPost } from "@/api/axios";
import type { ApiResponse, CursorPageResult } from "@/types/api";
import type {
  HoldSeatsRequest,
  HoldOrderResponse,
  CheckoutRequest,
  CheckoutResponse,
  CancelOrderRequest,
  CancelOrderResponse,
  ApplyPromotionRequest,
  OrderPromotionResponse,
  OrderListParams,
  OrderListItemDto,
  OrderDetailDto,
  OrderExchangePreviewParams,
  OrderExchangePreviewResponse,
  StartOrderExchangeRequest,
  StartOrderExchangeResponse,
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

  applyPromotion: (orderId: number, body: ApplyPromotionRequest) =>
    apiPost<ApiResponse<OrderPromotionResponse>>(ENDPOINTS.orders.promotion(orderId), body),

  removePromotion: (orderId: number) =>
    apiDelete<ApiResponse<OrderPromotionResponse>>(ENDPOINTS.orders.promotion(orderId)),

  cancelOrder: (orderId: number, body: CancelOrderRequest) =>
    apiPost<ApiResponse<CancelOrderResponse>>(ENDPOINTS.orders.cancel(orderId), body),

  previewExchange: (orderId: number, params: OrderExchangePreviewParams) =>
    apiGet<ApiResponse<OrderExchangePreviewResponse>>(ENDPOINTS.orders.exchangePreview(orderId), {
      params,
      paramsSerializer: { indexes: null },
    }),

  startExchange: (orderId: number, body: StartOrderExchangeRequest) =>
    apiPost<ApiResponse<StartOrderExchangeResponse>>(ENDPOINTS.orders.exchange(orderId), body),
};
