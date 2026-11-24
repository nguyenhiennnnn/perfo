import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import {
  ShowtimeRoomChangeContent,
  type RoomChangeShowtime,
} from "../components/room-change/showtime-room-change-content";

export function useShowtimeRoomChangeFlow(onChanged?: (showtimeId: number) => void) {
  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  const start = (showtime: RoomChangeShowtime) => {
    openDialog({
      title: "Chuyển suất sang phòng khác",
      description: "Chọn phòng đích, kiểm tra ánh xạ ghế rồi xác nhận. Đơn của khách được giữ nguyên.",
      size: "xl",
      className: "sm:max-w-5xl",
      content: (
        <ShowtimeRoomChangeContent
          showtime={showtime}
          onChanged={() => onChanged?.(showtime.showtimeId)}
          onClose={closeDialog}
        />
      ),
    });
  };

  return { start };
}
