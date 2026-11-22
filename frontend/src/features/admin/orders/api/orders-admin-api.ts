import { apiGet, apiPost } from "@/api/axios";
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

export const ordersAdminApi = {
  getOrders: (params: AdminOrderListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminOrderListItemDto>>>(
      ENDPOINTS.admin.bookings.root,
      { params },
    ),

  getOrderDetail: (orderId: number) =>
    apiGet<ApiResponse<OrderDetailDto>>(ENDPOINTS.admin.bookings.byId(orderId)),

  cancelOrder: (orderId: number, reason: CancelReason) =>
    apiPost<ApiResponse<CancelOrderResponse>>(
      ENDPOINTS.admin.bookings.counterCancel(orderId),
      { reason } satisfies CancelOrderRequest,
    ),
};
