import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { ledgerAdjustmentsAdminApi } from "../api/ledger-adjustments-admin-api";
import type { CreateLedgerAdjustmentRequest, LedgerAdjustmentListParams } from "../types";

export function useAdminLedgerAdjustments(params: LedgerAdjustmentListParams) {
  return useQuery({
    queryKey: queryKeys.adminLedgerAdjustments.list(params),
    queryFn: () => ledgerAdjustmentsAdminApi.getAdjustments(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminCreateLedgerAdjustment() {
  return useMutation({
    meta: { invalidates: "ledgerAdjustmentChanged" },
    mutationFn: (body: CreateLedgerAdjustmentRequest) =>
      ledgerAdjustmentsAdminApi.create(body),
  });
}
