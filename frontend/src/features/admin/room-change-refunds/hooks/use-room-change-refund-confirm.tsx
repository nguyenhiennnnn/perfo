import { useQueryClient } from "@tanstack/react-query";

import { queryKeys } from "@/api/query-keys";
import { toast } from "@/components/ui/toast";
import { formatCurrency } from "@/lib/formatters";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { RoomChangeRefundResult } from "../components/room-change-refund-result";
import type { RoomChangeRefundListItemDto } from "../types";
import { useConfirmRoomChangeRefund } from "./use-room-change-refunds";

export function useRoomChangeRefundConfirm() {
  const confirmRefund = useConfirmRoomChangeRefund();
  const openAlertDialog = useAlertDialogOpen();
  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const queryClient = useQueryClient();

  return (item: RoomChangeRefundListItemDto) => {
    openAlertDialog({
      title: "Xác nhận hoàn chênh lệch đổi phòng?",
      description: `Hoàn ${formatCurrency(item.amountDue)} cho đơn ${item.orderCode} (ghế ${item.fromSeat} → ${item.toSeat}). Hệ thống ghi nhận giao dịch hoàn theo phương thức khách đã thanh toán và không thể hoàn tác.`,
      confirmText: "Xác nhận hoàn",
      onConfirm: async () => {
        try {
          const res = await confirmRefund.mutateAsync(item.roomChangeRefundDueId);
          const result = res.data;
          if (!result) return;

          if (result.alreadyRefunded) {
            toast.add({
              title: "Khoản hoàn đã được xác nhận trước đó",
              description: `Đơn ${result.orderCode} · ${formatCurrency(result.amount)} đã được hoàn, không cần thao tác thêm.`,
              type: "info",
            });
            return;
          }

          openDialog({
            title: "Kết quả hoàn chênh lệch",
            size: "md",
            content: <RoomChangeRefundResult result={result} onClose={closeDialog} />,
          });
        } catch {
          await queryClient.invalidateQueries({
            queryKey: queryKeys.adminRoomChangeRefunds.all(),
          });
        }
      },
    });
  };
}
