import { useDialogOpen } from "@/stores/dialog-store";

import { ShowtimeSeatManager } from "../components/seat-blocks/showtime-seat-manager";
import type { AdminShowtimeDto } from "../types";

export function useShowtimeSeatManagerFlow() {
  const openDialog = useDialogOpen();

  const start = (showtime: AdminShowtimeDto) => {
    openDialog({
      title: "Quản lý ghế",
      description: `Khóa ghế hỏng hoặc đang bảo trì của suất chiếu phim "${showtime.movieTitle}".`,
      size: "xl",
      className: "sm:max-w-5xl",
      content: <ShowtimeSeatManager showtimeId={showtime.showtimeId} />,
    });
  };

  return { start };
}
