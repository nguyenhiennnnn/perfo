import { useRef } from "react";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import type { CancelReason } from "@/types/enum";
import { OrderCancelReasonPicker } from "../components/order-cancel-reason-picker";
import { useCancelOrderInline } from "./use-admin-orders";

interface CancellableOrder {
  orderId: number;
  orderCode: string;
}

export function useOrderCancelFlow() {
  const cancel = useCancelOrderInline();
  const openAlertDialog = useAlertDialogOpen();
  const reasonRef = useRef<CancelReason | null>(null);

  const start = (order: CancellableOrder, onError?: () => void) => {
    reasonRef.current = null;
    cancel.reset();
    openAlertDialog({
      title: `Hủy đơn ${order.orderCode}?`,
      description:
        "Thao tác không thể hoàn tác. Số tiền hoàn do hệ thống tính theo chính sách của rạp.",
      content: (
        <OrderCancelReasonPicker
          onReasonChange={(reason) => {
            reasonRef.current = reason;
          }}
        />
      ),
      confirmDisabled: true,
      confirmText: "Xác nhận hủy đơn",
      cancelText: "Giữ lại đơn",
      variant: "destructive",
      onConfirm: async () => {
        const reason = reasonRef.current;
        if (!reason) return;
        try {
          await cancel.mutateAsync({ orderId: order.orderId, reason });
        } catch {
          onError?.();
        }
      },
    });
  };

  return { cancel, start };
}
