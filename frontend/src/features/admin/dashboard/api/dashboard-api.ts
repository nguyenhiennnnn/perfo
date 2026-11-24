import { apiGet } from "@/api/axios";
import type { ApiResponse } from "@/types/api";
import type {
  DashboardSummaryDto,
  DashboardInsightsDto,
  DashboardInsightsParams,
  DashboardOccupancyDto,
  DashboardOccupancyParams,
  DashboardSeatHeatmapDto,
  DashboardSeatHeatmapParams,
} from "@/features/admin/dashboard/types";
import { ENDPOINTS } from "@/api/endpoints";

export const dashboardApi = {
  getSummary: () => apiGet<ApiResponse<DashboardSummaryDto>>(ENDPOINTS.admin.dashboard.summary),

  getInsights: (params: DashboardInsightsParams) =>
    apiGet<ApiResponse<DashboardInsightsDto>>(ENDPOINTS.admin.dashboard.insights, { params }),

  getOccupancy: (params: DashboardOccupancyParams) =>
    apiGet<ApiResponse<DashboardOccupancyDto>>(ENDPOINTS.admin.dashboard.occupancy, {
      params,
      paramsSerializer: { indexes: null },
    }),

  getSeatHeatmap: (params: DashboardSeatHeatmapParams) =>
    apiGet<ApiResponse<DashboardSeatHeatmapDto>>(ENDPOINTS.admin.dashboard.seatHeatmap, {
      params,
    }),
};
