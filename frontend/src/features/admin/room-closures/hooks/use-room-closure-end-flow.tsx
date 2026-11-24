import { useAlertDialogOpen } from "@/stores/alert-dialog-store";

import type { DeleteRoomClosureResponse, RoomClosureDto } from "../types";
import { getClosureStatus } from "../utils/closure-status";
import { useDeleteRoomClosure } from "./use-room-closures";

export function useRoomClosureEndFlow(onEnded?: (closure: RoomClosureDto, result: DeleteRoomClosureResponse | null) => void) {
  const deleteClosure = useDeleteRoomClosure();
  const openAlertDialog = useAlertDialogOpen();

  const start = (closure: RoomClosureDto) => {
    const isUpcoming = getClosureStatus(closure) === "upcoming";
    openAlertDialog({
      title: isUpcoming
        ? `Hủy lịch đóng phòng "${closure.roomName}"?`
        : `Mở lại phòng "${closure.roomName}" sớm?`,
      description: isUpcoming
        ? "Lịch đóng sẽ bị xóa và phòng chiếu hoạt động bình thường trong khoảng thời gian này."
        : "Phòng chiếu mở lại từ thời điểm hiện tại. Các suất đã chuyển phòng hoặc đã hủy trước đó không được khôi phục.",
      confirmText: isUpcoming ? "Hủy lịch đóng" : "Mở lại phòng",
      cancelText: "Giữ nguyên",
      variant: "destructive",
      onConfirm: async () => {
        const res = await deleteClosure.mutateAsync(closure.roomClosureId);
        onEnded?.(closure, res.data ?? null);
      },
    });
  };

  return { start, isPending: deleteClosure.isPending };
}
