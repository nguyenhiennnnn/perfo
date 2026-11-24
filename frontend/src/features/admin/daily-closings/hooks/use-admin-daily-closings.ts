import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { dailyClosingsAdminApi } from "../api/daily-closings-admin-api";
import type {
  CloseDailyClosingRequest,
  DailyClosingListParams,
  DailyClosingReportMode,
} from "../types";

export function useAdminDailyClosing(date: string) {
  return useQuery({
    queryKey: queryKeys.adminDailyClosings.detail(date),
    queryFn: () => dailyClosingsAdminApi.getByDate(date),
    retry: false,
    select: (res) => res.data,
  });
}

export function useAdminDailyClosingDraft(date: string) {
  return useQuery({
    queryKey: queryKeys.adminDailyClosings.draft(date),
    queryFn: () => dailyClosingsAdminApi.getDraft(date),
    select: (res) => res.data,
  });
}

export function useAdminCloseDailyClosing() {
  return useMutation({
    meta: { invalidates: "dailyClosingChanged" },
    mutationFn: ({ date, ...body }: CloseDailyClosingRequest & { date: string }) =>
      dailyClosingsAdminApi.close(date, body),
  });
}

export function useAdminDailyClosingList(params: DailyClosingListParams) {
  return useQuery({
    queryKey: queryKeys.adminDailyClosings.list(params),
    queryFn: () => dailyClosingsAdminApi.getClosings(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminDailyClosingReport(date: string, mode: DailyClosingReportMode) {
  return useQuery({
    queryKey: queryKeys.adminDailyClosings.report(date, mode),
    queryFn: () => dailyClosingsAdminApi.getReport(date, mode),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}
