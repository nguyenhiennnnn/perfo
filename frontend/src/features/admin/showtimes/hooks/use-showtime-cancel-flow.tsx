import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { ShowtimeCancelConsequences } from "../components/showtime-cancel-consequences";
import { ShowtimeCancelResult } from "../components/showtime-cancel-result";
import type { AdminShowtimeDto, ShowtimeCancelReason } from "../types";
import { useAdminDeleteShowtime } from "./use-admin-showtimes";

interface ShowtimeCancelFlowOptions {
  reason?: ShowtimeCancelReason;
}

type CancellableShowtime = Pick<AdminShowtimeDto, "showtimeId" | "movieTitle">;

export function useShowtimeCancelFlow(
  onCancelled?: (showtimeId: number) => void,
  options?: ShowtimeCancelFlowOptions,
) {
  const cancel = useAdminDeleteShowtime();
  const openAlertDialog = useAlertDialogOpen();
  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  const start = (showtime: CancellableShowtime) => {
    openAlertDialog({
      title: `Hủy suất chiếu "${showtime.movieTitle}"?`,
      description: "Thao tác không thể hoàn tác. Hệ quả của việc hủy suất:",
      content: <ShowtimeCancelConsequences />,
      confirmText: "Hủy suất chiếu",
      cancelText: "Giữ lại suất chiếu",
      variant: "destructive",
      onConfirm: async () => {
        const res = await cancel.mutateAsync({
          id: showtime.showtimeId,
          reason: options?.reason,
        });
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
