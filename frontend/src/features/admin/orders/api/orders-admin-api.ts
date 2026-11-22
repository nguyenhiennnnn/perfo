import { apiGet } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AdminOrderListItemDto,
  AdminOrderListParams,
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
};
