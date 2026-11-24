import { apiGet, apiPatch, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type { ImportExportJobAccepted } from "@/types/import-export";
import type {
  ReconciliationItemDto,
  ReconciliationItemListParams,
  ReconciliationSessionDto,
  ReconciliationSessionListParams,
  StripeReconcileParams,
  UpdateReconciliationItemRequest,
} from "../types";

export const reconciliationApi = {
  getSessions: (params: ReconciliationSessionListParams) =>
    apiGet<ApiResponse<OffsetPageResult<ReconciliationSessionDto>>>(
      ENDPOINTS.admin.reconciliation.sessions,
      { params },
    ),

  getSession: (id: number) =>
    apiGet<ApiResponse<ReconciliationSessionDto>>(ENDPOINTS.admin.reconciliation.sessionById(id)),

  getItems: (params: ReconciliationItemListParams) =>
    apiGet<ApiResponse<OffsetPageResult<ReconciliationItemDto>>>(ENDPOINTS.admin.reconciliation.items, {
      params,
    }),

  updateItem: (id: number, body: UpdateReconciliationItemRequest) =>
    apiPatch<ApiResponse<ReconciliationItemDto>>(ENDPOINTS.admin.reconciliation.itemById(id), body),

  reconcileStripe: (params: StripeReconcileParams) =>
    apiPost<ApiResponse<ImportExportJobAccepted>>(ENDPOINTS.admin.reconciliation.stripe, undefined, {
      params,
    }),
};
