export interface RevenueReportParams {
  fromDate: string;
  toDate: string;
  groupBy: string;
}

export interface RevenueReportItemDto {
  period: string;
  ticketRevenue: number;
  comboRevenue: number;
  totalRevenue: number;
  bookingCount: number;
}

export interface RevenueSummaryDto {
  totalRevenue: number;
  totalBookings: number;
}

export interface RevenueReportDto {
  groupBy: string;
  items: RevenueReportItemDto[];
  summary: RevenueSummaryDto;
}

export interface TopMovieReportParams {
  fromDate: string;
  toDate: string;
  limit?: number;
}

export interface TopMovieDto {
  movieId: number;
  title: string;
  ticketsSold: number;
  revenue: number;
}

export interface ReportDateParams {
  fromDate: string;
  toDate: string;
}

export interface EmployeePerformanceDto {
  employeeId: number;
  fullName: string;
  ticketsSold: number;
  revenue: number;
  bookingCount: number;
}

export interface FinanceReportParams {
  from: string;
  to: string;
  blocks?: string;
}

export type FinanceColumnType = "Text" | "Number" | "Money" | "Percent";

export interface FinanceReportColumnDto {
  key: string;
  title: string;
  type: FinanceColumnType | string;
}

export type FinanceReportCell = string | number | boolean | null | undefined;

export interface FinanceReportBlockDto {
  key: string;
  title: string;
  columns: FinanceReportColumnDto[];
  rows: Record<string, FinanceReportCell>[];
  totals?: Record<string, FinanceReportCell> | null;
  warnings?: string[] | null;
}

export interface FinanceReportDto {
  from: string;
  to: string;
  blocks: FinanceReportBlockDto[];
}

export interface RoomQualityAlertDto {
  roomQualityAlertId: number;
  roomId: number;
  roomName: string;
  fromDate: string;
  toDate: string;
  consecutiveDays: number;
  averageScore: number;
  threshold: number;
  detectedAt: string;
  notifiedAt?: string | null;
  resolvedAt?: string | null;
}
