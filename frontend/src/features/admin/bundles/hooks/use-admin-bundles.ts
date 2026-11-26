import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { bundlesAdminApi } from "../api/bundles-admin-api";
import type { BundleListParams, UpsertBundleRequest } from "../types";

export function useAdminBundles(params: BundleListParams) {
  return useQuery({
    queryKey: queryKeys.adminBundles.list(params),
    queryFn: () => bundlesAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminCreateBundle() {
  return useMutation({
    meta: { invalidates: "bundleChanged", skipErrorToast: true },
    mutationFn: (body: UpsertBundleRequest) => bundlesAdminApi.create(body),
  });
}

export function useAdminUpdateBundle() {
  return useMutation({
    meta: { invalidates: "bundleChanged", skipErrorToast: true },
    mutationFn: ({ id, body }: { id: number; body: UpsertBundleRequest }) =>
      bundlesAdminApi.update(id, body),
  });
}

export function useAdminDeleteBundle() {
  return useMutation({
    meta: { invalidates: "bundleChanged" },
    mutationFn: (id: number) => bundlesAdminApi.delete(id),
  });
}
