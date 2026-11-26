import { useQueryClient } from "@tanstack/react-query";

import { queryKeys } from "@/api/query-keys";
import { formatDateTime } from "@/lib/formatters";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";

import type { CustomerRestrictionDto } from "../types";
import { useLiftCustomerRestriction } from "./use-customer-restrictions";

export function useCustomerRestrictionLift() {
  const liftRestriction = useLiftCustomerRestriction();
  const openAlertDialog = useAlertDialogOpen();
  const queryClient = useQueryClient();

  return (item: CustomerRestrictionDto) => {
    openAlertDialog({
      title: `Gỡ hạn chế của "${item.customerName}"?`,
      description: `Hạn chế hiện kéo dài đến ${formatDateTime(item.expiresAt)}. Sau khi gỡ, khách được giữ ghế như bình thường và bộ đếm đơn hết hạn liên tiếp được tính lại từ thời điểm này. Thao tác được ghi vào nhật ký.`,
      confirmText: "Gỡ hạn chế",
      onConfirm: async () => {
        try {
          await liftRestriction.mutateAsync(item.customerRestrictionId);
        } catch {
          await queryClient.invalidateQueries({
            queryKey: queryKeys.adminCustomerRestrictions.all(),
          });
        }
      },
    });
  };
}
