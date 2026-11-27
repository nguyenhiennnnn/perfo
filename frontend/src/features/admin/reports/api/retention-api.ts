import { apiGet } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { RetentionReportDto } from "../types/retention";

export const retentionReportApi = {
  get: (months: number) =>
    apiGet<ApiResponse<RetentionReportDto>>(ENDPOINTS.admin.reports.retention, {
      params: { months },
    }),
};
