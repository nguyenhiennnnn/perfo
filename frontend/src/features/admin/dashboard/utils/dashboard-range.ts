import { useAdminDashboardOccupancyParams } from "@/hooks/use-query-params";
import { getDefaultReportRange } from "@/features/admin/reports/utils/default-range";

const DEFAULT_RANGE_DAYS = 14;

/** Khoảng ngày dùng chung của dashboard (from/to trên URL). */
export const useDashboardRange = () => {
  const [params] = useAdminDashboardOccupancyParams();
  const defaultRange = getDefaultReportRange(DEFAULT_RANGE_DAYS);
  return {
    from: params.from ?? defaultRange.fromDate,
    to: params.to ?? defaultRange.toDate,
  };
};
