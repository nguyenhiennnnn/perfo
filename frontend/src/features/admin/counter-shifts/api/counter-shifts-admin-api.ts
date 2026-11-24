import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AdminCloseCounterShiftRequest,
  CounterShift,
  CounterShiftListParams,
  OpenCounterShiftItem,
} from "../types";

export const counterShiftsAdminApi = {
  getShifts: (params: CounterShiftListParams) =>
    apiGet<ApiResponse<OffsetPageResult<CounterShift>>>(ENDPOINTS.admin.counterShifts.root, {
      params,
    }),

  getOpenShifts: () =>
    apiGet<ApiResponse<OpenCounterShiftItem[]>>(ENDPOINTS.admin.counterShifts.open),

  closeByAdmin: (id: number, body: AdminCloseCounterShiftRequest) =>
    apiPost<ApiResponse<CounterShift>>(ENDPOINTS.admin.counterShifts.closeByAdmin(id), body),
};
