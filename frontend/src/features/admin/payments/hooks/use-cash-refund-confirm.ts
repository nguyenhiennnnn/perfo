import { useQueryClient } from "@tanstack/react-query";

import { queryKeys } from "@/api/query-keys";
import { toast } from "@/components/ui/toast";
import { formatCurrency } from "@/lib/formatters";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAuthUser } from "@/stores/auth-store";

import type { CashRefundListItemDto } from "../types/cash-refunds";
import { CashRefundStatus } from "../types/cash-refunds";
import { useConfirmCashRefund } from "./use-payment-ledger";

export function useCashRefundConfirm() {
  const confirmRefund = useConfirmCashRefund();
  const openAlertDialog = useAlertDialogOpen();
  const queryClient = useQueryClient();
  const user = useAuthUser();

  return (item: CashRefundListItemDto) => {
    openAlertDialog({
      title: "Xác nhận đã hoàn tiền mặt?",
      description: `Bạn đã trả ${formatCurrency(item.amount)} tiền mặt cho ${
        item.customerName || "khách vãng lai"
      } (đơn ${item.orderCode}). Thao tác này ghi nhận giao dịch hoàn và không thể hoàn tác.`,
      confirmText: "Đã hoàn tiền",
      onConfirm: async () => {
        try {
          const res = await confirmRefund.mutateAsync(item.refundTransactionId);
          const result = res.data;
          if (result?.status !== CashRefundStatus.Refunded) return;

          if (result.confirmedByUserId != null && result.confirmedByUserId !== user?.userId) {
            toast.add({
              title: "Khoản hoàn đã được xác nhận trước đó",
              description: `${result.confirmedByName ?? "Một nhân viên khác"} đã xác nhận hoàn tiền cho đơn ${result.orderCode}.`,
              type: "info",
            });
            return;
          }

          toast.add({
            title: "Đã xác nhận hoàn tiền mặt",
            description: `Đơn ${result.orderCode} · ${formatCurrency(result.amount)}.`,
            type: "success",
          });
        } catch {
          await queryClient.invalidateQueries({ queryKey: queryKeys.adminCashRefunds.all() });
        }
      },
    });
  };
}
