import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { dashboardApi } from "../api/dashboard-api";
import { queryKeys } from "@/api/query-keys";
import type {
  DashboardInsightsParams,
  DashboardOccupancyParams,
  DashboardSeatHeatmapParams,
} from "@/features/admin/dashboard/types";
import { DASHBOARD_REFETCH_INTERVAL_MS } from "@/config/default-constants";

export function useDashboardSummary() {
  return useQuery({
    queryKey: queryKeys.adminDashboard.summary(),
    queryFn: () => dashboardApi.getSummary(),
    select: (res) => res.data,
    refetchInterval: DASHBOARD_REFETCH_INTERVAL_MS,
  });
}

export function useDashboardInsights(params: DashboardInsightsParams = {}) {
  return useQuery({
    queryKey: queryKeys.adminDashboard.insights(params),
    queryFn: () => dashboardApi.getInsights(params),
    select: (res) => res.data,
  });
}

export function useDashboardOccupancy(params: DashboardOccupancyParams) {
  return useQuery({
    queryKey: queryKeys.adminDashboard.occupancy(params),
    queryFn: () => dashboardApi.getOccupancy(params),
    select: (res) => res.data,
    placeholderData: keepPreviousData,
  });
}

export function useDashboardSeatHeatmap(params: DashboardSeatHeatmapParams | null) {
  return useQuery({
    queryKey: queryKeys.adminDashboard.seatHeatmap(params ?? {}),
    queryFn: () => dashboardApi.getSeatHeatmap(params!),
    select: (res) => res.data,
    placeholderData: keepPreviousData,
    enabled: params != null,
  });
}
