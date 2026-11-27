import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { ShowtimeEventManager } from "../components/event/showtime-event-manager";
import type { AdminShowtimeDto } from "../types";

export function useShowtimeEventFlow() {
  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  const start = (showtime: AdminShowtimeDto) => {
    openDialog({
      title: "Suất chiếu sự kiện",
      description: `Phụ thu, hạn mức vé, mở bán sớm và quà tặng kèm cho suất chiếu phim "${showtime.movieTitle}".`,
      size: "xl",
      content: (
        <ShowtimeEventManager showtimeId={showtime.showtimeId} onClose={closeDialog} />
      ),
    });
  };

  return { start };
}
