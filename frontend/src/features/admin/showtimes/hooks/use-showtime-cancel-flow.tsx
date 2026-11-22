import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { ShowtimeCancelConsequences } from "../components/showtime-cancel-consequences";
import { ShowtimeCancelResult } from "../components/showtime-cancel-result";
import type { AdminShowtimeDto } from "../types";
import { useAdminDeleteShowtime } from "./use-admin-showtimes";

export function useShowtimeCancelFlow(onCancelled?: (showtimeId: number) => void) {
  const cancel = useAdminDeleteShowtime();
  const openAlertDialog = useAlertDialogOpen();
  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  const start = (showtime: AdminShowtimeDto) => {
    openAlertDialog({
      title: `Hủy suất chiếu "${showtime.movieTitle}"?`,
      description: "Thao tác không thể hoàn tác. Hệ quả của việc hủy suất:",
      content: <ShowtimeCancelConsequences />,
      confirmText: "Hủy suất chiếu",
      cancelText: "Giữ lại suất chiếu",
      variant: "destructive",
      onConfirm: async () => {
        const res = await cancel.mutateAsync(showtime.showtimeId);
        onCancelled?.(showtime.showtimeId);
        if (!res.data) return;
        openDialog({
          title: "Kết quả hủy suất chiếu",
          content: (
            <ShowtimeCancelResult
              movieTitle={showtime.movieTitle}
              result={res.data}
              onNavigate={closeDialog}
            />
          ),
        });
      },
    });
  };

  return { cancel, start };
}
