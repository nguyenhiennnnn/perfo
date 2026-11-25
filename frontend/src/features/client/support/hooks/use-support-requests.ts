import { useInfiniteQuery, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { SupportRequestStatus } from "@/types/enum";
import { SUPPORT_DETAIL_POLL_INTERVAL_MS } from "../constants";
import { supportApi } from "../api/support-api";
import type {
  CreateSupportRequestRequest,
  ReplySupportRequestRequest,
  SupportRequestListParams,
} from "../types";

export function useMySupportRequests(params: Omit<SupportRequestListParams, "page">) {
  return useInfiniteQuery({
    queryKey: queryKeys.support.list(params),
    queryFn: ({ pageParam }) => supportApi.getList({ ...params, page: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.data.page < lastPage.data.totalPages ? lastPage.data.page + 1 : undefined,
    select: (data) => ({
      requests: data.pages.flatMap((p) => p.data.items),
    }),
  });
}

export function useSupportRequestDetail(requestId: number) {
  return useQuery({
    queryKey: queryKeys.support.detail(requestId),
    queryFn: () => supportApi.getById(requestId),
    enabled: requestId > 0,
    refetchInterval: (query) =>
      query.state.data?.data.status === SupportRequestStatus.Resolved
        ? false
        : SUPPORT_DETAIL_POLL_INTERVAL_MS,
    select: (res) => res.data,
  });
}

export function useCreateSupportRequest() {
  return useMutation({
    meta: { invalidates: "supportRequestChanged" },
    mutationFn: (body: CreateSupportRequestRequest) => supportApi.create(body),
  });
}

export function useReplySupportRequest(requestId: number) {
  return useMutation({
    meta: { invalidates: "supportRequestChanged" },
    mutationFn: (body: ReplySupportRequestRequest) => supportApi.reply(requestId, body),
  });
}
