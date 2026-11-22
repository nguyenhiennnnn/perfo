import { apiGet } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  CounterOrderDetail,
  CounterOrderListItem,
  CounterOrderSearchParams,
} from "../types/counter-cancel";

export const counterCancelApi = {
  searchOrders: (params: CounterOrderSearchParams) =>
    apiGet<ApiResponse<OffsetPageResult<CounterOrderListItem>>>(
      ENDPOINTS.admin.bookings.root,
      { params },
    ),

  getOrderDetail: (orderId: number) =>
    apiGet<ApiResponse<CounterOrderDetail>>(
      ENDPOINTS.admin.bookings.byId(orderId),
    ),
};
