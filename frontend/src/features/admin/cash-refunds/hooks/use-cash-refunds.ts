import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { cashRefundsApi } from "../api/cash-refunds-api";
import type { CashRefundListParams } from "../types";

export function useCashRefunds(params: CashRefundListParams) {
  return useQuery({
    queryKey: queryKeys.adminCashRefunds.list(params),
    queryFn: () => cashRefundsApi.getCashRefunds(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useConfirmCashRefund() {
  return useMutation({
    meta: { invalidates: "bookingChanged", skipSuccessToast: true },
    mutationFn: (refundTransactionId: number) =>
      cashRefundsApi.confirmCashRefund(refundTransactionId),
  });
}
