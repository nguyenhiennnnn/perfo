import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { retentionReportApi } from "../api/retention-api";

export function useRetentionReport(months: number) {
  return useQuery({
    queryKey: queryKeys.adminReports.retention(months),
    queryFn: () => retentionReportApi.get(months),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}
