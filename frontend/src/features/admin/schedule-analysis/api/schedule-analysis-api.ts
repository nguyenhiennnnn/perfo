import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type {
  ApplyScheduleSuggestionsRequest,
  ApplyScheduleSuggestionsResponseDto,
  ScheduleReportDto,
  ScheduleReportParams,
  ScheduleSuggestionsDto,
} from "../types";

export const scheduleAnalysisApi = {
  getReport: (params: ScheduleReportParams) =>
    apiGet<ApiResponse<ScheduleReportDto>>(ENDPOINTS.admin.scheduleAnalysis.report, { params }),

  getSuggestions: () =>
    apiGet<ApiResponse<ScheduleSuggestionsDto>>(ENDPOINTS.admin.scheduleAnalysis.suggestions),

  applySuggestions: (body: ApplyScheduleSuggestionsRequest) =>
    apiPost<ApiResponse<ApplyScheduleSuggestionsResponseDto>>(
      ENDPOINTS.admin.scheduleAnalysis.applySuggestions,
      body,
    ),
};
