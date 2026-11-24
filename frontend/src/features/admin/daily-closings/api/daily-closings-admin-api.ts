import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  CloseDailyClosingRequest,
  DailyClosingDto,
  DailyClosingListItem,
  DailyClosingListParams,
  DailyClosingReport,
  DailyClosingReportMode,
  DailyClosingSnapshot,
} from "../types";

export const dailyClosingsAdminApi = {
  getClosings: (params: DailyClosingListParams) =>
    apiGet<ApiResponse<OffsetPageResult<DailyClosingListItem>>>(
      ENDPOINTS.admin.dailyClosings.root,
      { params },
    ),

  getByDate: (date: string) =>
    apiGet<ApiResponse<DailyClosingDto>>(ENDPOINTS.admin.dailyClosings.byDate(date)),

  getReport: (date: string, mode: DailyClosingReportMode) =>
    apiGet<ApiResponse<DailyClosingReport>>(ENDPOINTS.admin.dailyClosings.report(date), {
      params: { mode },
    }),

  getDraft: (date: string) =>
    apiGet<ApiResponse<DailyClosingSnapshot>>(ENDPOINTS.admin.dailyClosings.draft, {
      params: { date },
    }),

  close: (date: string, body: CloseDailyClosingRequest) =>
    apiPost<ApiResponse<DailyClosingDto>>(ENDPOINTS.admin.dailyClosings.close(date), body),
};
