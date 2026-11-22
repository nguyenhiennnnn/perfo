import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AdminPaymentDetailDto,
  AdminPaymentLedgerParams,
  AdminPaymentListItemDto,
} from "../types/ledger";
import type {
  CashRefundConfirmResultDto,
  CashRefundListItemDto,
  CashRefundListParams,
} from "../types/cash-refunds";

export const paymentLedgerApi = {
  getTransactions: (params: AdminPaymentLedgerParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminPaymentListItemDto>>>(
      ENDPOINTS.admin.payments.root,
      { params },
    ),

  getTransactionDetail: (transactionId: number) =>
    apiGet<ApiResponse<AdminPaymentDetailDto>>(
      ENDPOINTS.admin.payments.byId(transactionId),
    ),

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
