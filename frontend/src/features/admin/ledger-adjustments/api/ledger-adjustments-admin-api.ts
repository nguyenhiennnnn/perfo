import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  CreateLedgerAdjustmentRequest,
  LedgerAdjustment,
  LedgerAdjustmentListParams,
} from "../types";

export const ledgerAdjustmentsAdminApi = {
  getAdjustments: (params: LedgerAdjustmentListParams) =>
    apiGet<ApiResponse<OffsetPageResult<LedgerAdjustment>>>(
      ENDPOINTS.admin.ledgerAdjustments.root,
      { params },
    ),

  create: (body: CreateLedgerAdjustmentRequest) =>
    apiPost<ApiResponse<LedgerAdjustment>>(ENDPOINTS.admin.ledgerAdjustments.root, body),
};
