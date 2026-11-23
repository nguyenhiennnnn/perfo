import { useCallback } from "react";
import { PriceChangeContent, type PriceChangeSeatInfo } from "@/components/common/price-change-content";
import type { PriceChangeRequired } from "@/lib/error";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";

export function usePriceChangeConfirm() {
  const openAlertDialog = useAlertDialogOpen();

  return useCallback(
    (change: PriceChangeRequired, seats: PriceChangeSeatInfo[], previousTotal: number) =>
      new Promise<boolean>((resolve) => {
        openAlertDialog({
          title: "Giá vé đã thay đổi",
          description:
            "Giá vé của suất này vừa được cập nhật so với giá bạn đang xem. Vui lòng xác nhận giá mới để tiếp tục giữ ghế.",
          content: (
            <PriceChangeContent change={change} seats={seats} previousTotal={previousTotal} />
          ),
          confirmText: "Đồng ý giá mới",
          cancelText: "Chọn lại",
          onConfirm: () => resolve(true),
          onCancel: () => resolve(false),
        });
      }),
    [openAlertDialog],
  );
}
