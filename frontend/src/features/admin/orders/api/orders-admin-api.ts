import { apiGet, apiPost, apiPostKeepalive } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type { CancelReason } from "@/types/enum";
import type {
  AdminOrderListItemDto,
  AdminOrderListParams,
  CancelOrderRequest,
  CancelOrderResponse,
  OrderDetailDto,
} from "../types";
import type {
  OrderExchangePreviewParams,
  OrderExchangePreviewResponse,
  StartOrderExchangeRequest,
  StartOrderExchangeResponse,
} from "@/features/client/orders/types";

export const ordersAdminApi = {
  getOrders: (params: AdminOrderListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminOrderListItemDto>>>(
      ENDPOINTS.admin.orders.root,
      { params },
    ),

  getOrderDetail: (orderId: number) =>
    apiGet<ApiResponse<OrderDetailDto>>(ENDPOINTS.admin.orders.byId(orderId)),

  cancelOrder: (orderId: number, reason: CancelReason) =>
    apiPost<ApiResponse<CancelOrderResponse>>(
      ENDPOINTS.admin.orders.counterCancel(orderId),
      { reason } satisfies CancelOrderRequest,
    ),

  previewExchange: (orderId: number, params: OrderExchangePreviewParams) =>
    apiGet<ApiResponse<OrderExchangePreviewResponse>>(
      ENDPOINTS.admin.orders.counterExchangePreview(orderId),
      { params, paramsSerializer: { indexes: null } },
    ),

  startExchange: (orderId: number, body: StartOrderExchangeRequest) =>
    apiPost<ApiResponse<StartOrderExchangeResponse>>(
      ENDPOINTS.admin.orders.counterExchange(orderId),
      body,
    ),

  cancelOrderKeepalive: (orderId: number, reason: CancelReason) =>
    apiPostKeepalive(ENDPOINTS.admin.orders.counterCancel(orderId), {
      reason,
    } satisfies CancelOrderRequest),
};
