import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { trackImportExportJob } from "@/features/admin/import-export/lib/job-sync";
import { reconciliationApi } from "../api/reconciliation-api";
import type {
  ReconciliationItemListParams,
  ReconciliationSessionListParams,
  StripeReconcileParams,
  UpdateReconciliationItemRequest,
} from "../types";

export function useReconciliationSessions(params: ReconciliationSessionListParams) {
  return useQuery({
    queryKey: queryKeys.adminReconciliation.sessions(params),
    queryFn: () => reconciliationApi.getSessions(params),
    select: (res) => res.data,
    placeholderData: keepPreviousData,
  });
}

export function useReconciliationSession(id: number) {
  return useQuery({
    queryKey: queryKeys.adminReconciliation.session(id),
    queryFn: () => reconciliationApi.getSession(id),
    select: (res) => res.data,
    enabled: id > 0,
  });
}

export function useReconcileStripe() {
  return useMutation({
    meta: { errorTitle: "Không thể chạy đối soát Stripe", skipSuccessToast: true },
    mutationFn: (params: StripeReconcileParams) => reconciliationApi.reconcileStripe(params),
    onSuccess: (res) => {
      if (res.data.jobId) void trackImportExportJob(res.data.jobId);
    },
  });
}

export function useReconciliationItems(params: ReconciliationItemListParams) {
  return useQuery({
    queryKey: queryKeys.adminReconciliation.items(params),
    queryFn: () => reconciliationApi.getItems(params),
    select: (res) => res.data,
    placeholderData: keepPreviousData,
  });
}

export function useUpdateReconciliationItem() {
  return useMutation({
    meta: { invalidates: "reconciliationChanged", errorTitle: "Không thể cập nhật dòng bất thường" },
    mutationFn: ({ id, body }: { id: number; body: UpdateReconciliationItemRequest }) =>
      reconciliationApi.updateItem(id, body),
  });
}
