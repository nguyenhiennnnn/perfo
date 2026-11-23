import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  CashRefundConfirmResultDto,
  CashRefundListItemDto,
  CashRefundListParams,
} from "../types";

export const cashRefundsApi = {
  getCashRefunds: (params: CashRefundListParams) =>
    apiGet<ApiResponse<OffsetPageResult<CashRefundListItemDto>>>(
      ENDPOINTS.admin.cashRefunds.root,
      { params },
    ),

  confirmCashRefund: (refundTransactionId: number) =>
    apiPost<ApiResponse<CashRefundConfirmResultDto>>(
      ENDPOINTS.admin.cashRefunds.confirm(refundTransactionId),
    ),
};
