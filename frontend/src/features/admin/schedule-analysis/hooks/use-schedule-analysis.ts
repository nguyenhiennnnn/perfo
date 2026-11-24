import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { scheduleAnalysisApi } from "../api/schedule-analysis-api";
import type { ApplyScheduleSuggestionsRequest, ScheduleReportParams } from "../types";

export function useScheduleReport(params: ScheduleReportParams) {
  return useQuery({
    queryKey: queryKeys.adminScheduleAnalysis.report(params),
    queryFn: () => scheduleAnalysisApi.getReport(params),
    select: (res) => res.data,
    placeholderData: keepPreviousData,
  });
}

export function useScheduleSuggestions() {
  return useQuery({
    queryKey: queryKeys.adminScheduleAnalysis.suggestions(),
    queryFn: () => scheduleAnalysisApi.getSuggestions(),
    select: (res) => res.data,
  });
}

/** Chỉ xem trước: không tạo suất chiếu. */
export function useApplyScheduleSuggestions() {
  return useMutation({
    mutationFn: (body: ApplyScheduleSuggestionsRequest) =>
      scheduleAnalysisApi.applySuggestions(body),
  });
}
