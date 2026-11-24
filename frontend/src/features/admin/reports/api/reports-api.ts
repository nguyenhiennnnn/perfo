import { apiGet } from "@/api/axios";
import type { ApiResponse } from "@/types/api";
import type {
  RevenueReportParams,
  RevenueReportDto,
  TopMovieReportParams,
  TopMovieDto,
  ReportDateParams,
  EmployeePerformanceDto,
  FinanceReportDto,
  FinanceReportParams,
} from "../types";
import { ENDPOINTS } from "@/api/endpoints";

export const reportsApi = {
  getRevenue: (params: RevenueReportParams) =>
    apiGet<ApiResponse<RevenueReportDto>>(ENDPOINTS.admin.reports.revenue, { params }),

  getTopMovies: (params: TopMovieReportParams) =>
    apiGet<ApiResponse<TopMovieDto[]>>(ENDPOINTS.admin.reports.topMovies, { params }),

  getEmployeePerformance: (params: ReportDateParams) =>
    apiGet<ApiResponse<EmployeePerformanceDto[]>>(ENDPOINTS.admin.reports.employeePerformance, { params }),

  getFinance: (params: FinanceReportParams) =>
    apiGet<ApiResponse<FinanceReportDto>>(ENDPOINTS.admin.reports.finance, { params }),
};
