import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { paymentLedgerApi } from "../api/payment-ledger-api";
import type { AdminPaymentLedgerParams } from "../types/ledger";
import type { CashRefundListParams } from "../types/cash-refunds";

export function useAdminPaymentTransactions(params: AdminPaymentLedgerParams) {
  return useQuery({
    queryKey: queryKeys.adminPayments.list(params),
    queryFn: () => paymentLedgerApi.getTransactions(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminPaymentTransactionDetail(transactionId: number) {
  return useQuery({
    queryKey: queryKeys.adminPayments.detail(transactionId),
    queryFn: () => paymentLedgerApi.getTransactionDetail(transactionId),
    enabled: transactionId > 0,
    select: (res) => res.data,
  });
}

export function useCashRefunds(params: CashRefundListParams) {
  return useQuery({
    queryKey: queryKeys.adminCashRefunds.list(params),
    queryFn: () => paymentLedgerApi.getCashRefunds(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useConfirmCashRefund() {
  return useMutation({
    meta: { invalidates: "bookingChanged", skipSuccessToast: true },
    mutationFn: (refundTransactionId: number) =>
      paymentLedgerApi.confirmCashRefund(refundTransactionId),
  });
}
