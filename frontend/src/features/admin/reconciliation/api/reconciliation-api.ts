import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type { ImportExportJobAccepted } from "@/types/import-export";
import type {
  ReconciliationSessionDto,
  ReconciliationSessionListParams,
  StripeReconcileParams,
} from "../types";

export const reconciliationApi = {
  getSessions: (params: ReconciliationSessionListParams) =>
    apiGet<ApiResponse<OffsetPageResult<ReconciliationSessionDto>>>(
      ENDPOINTS.admin.reconciliation.sessions,
      { params },
    ),

  getSession: (id: number) =>
    apiGet<ApiResponse<ReconciliationSessionDto>>(ENDPOINTS.admin.reconciliation.sessionById(id)),

  reconcileStripe: (params: StripeReconcileParams) =>
    apiPost<ApiResponse<ImportExportJobAccepted>>(ENDPOINTS.admin.reconciliation.stripe, undefined, {
      params,
    }),
};
