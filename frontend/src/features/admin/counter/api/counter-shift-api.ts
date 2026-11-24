import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type {
  CloseCounterShiftRequest,
  CounterShift,
  OpenCounterShiftRequest,
} from "../types/counter-shift";

export const counterShiftApi = {
  getCurrent: () =>
    apiGet<ApiResponse<CounterShift | null>>(ENDPOINTS.admin.counterShifts.current),

  open: (body: OpenCounterShiftRequest) =>
    apiPost<ApiResponse<CounterShift>>(ENDPOINTS.admin.counterShifts.open, body),

  close: (id: number, body: CloseCounterShiftRequest) =>
    apiPost<ApiResponse<CounterShift>>(ENDPOINTS.admin.counterShifts.close(id), body),
};
