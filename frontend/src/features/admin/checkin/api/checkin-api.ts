import { apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { CounterOrderResponse } from "@/features/admin/counter/types";
import type {
  CheckinRequest,
  CheckinResponse,
  CounterSupplementOrderRequest,
  RecordCardNotPresentedResponse,
} from "../types";

export const checkinApi = {
  checkin: (body: CheckinRequest) =>
    apiPost<ApiResponse<CheckinResponse>>(ENDPOINTS.admin.orders.checkin, body),

  recordCardNotPresented: (orderLineId: number) =>
    apiPost<ApiResponse<RecordCardNotPresentedResponse>>(
      ENDPOINTS.admin.orders.cardNotPresented,
      { orderLineId },
    ),

  createSupplementOrder: (body: CounterSupplementOrderRequest) =>
    apiPost<ApiResponse<CounterOrderResponse>>(
      ENDPOINTS.admin.orders.counterSupplementOrder,
      body,
    ),
};
