import { ArrowRightLeft } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { getDisplayMessage } from "@/lib/error";
import { formatDate, formatTime } from "@/lib/formatters";
import { useBlockDialogClose } from "@/hooks/use-block-dialog-close";

import { useRoomChangePlan } from "../../hooks/use-room-change-plan";
import type { AdminShowtimeDto } from "../../types";
import { ROOM_CHANGE_NOTE_MAX_LENGTH } from "../../utils/room-change";
import { RoomChangeMappingTable } from "./room-change-mapping-table";
import { RoomChangeResult } from "./room-change-result";
import { RoomChangeSummary } from "./room-change-summary";
import { RoomChangeTargetPicker } from "./room-change-target-picker";

export type RoomChangeShowtime = Pick<
  AdminShowtimeDto,
  "showtimeId" | "movieTitle" | "roomId" | "roomName" | "startTime" | "endTime"
>;

interface ShowtimeRoomChangeContentProps {
  showtime: RoomChangeShowtime;
  onChanged?: () => void;
  onClose: () => void;
}

export const ShowtimeRoomChangeContent = ({
  showtime,
  onChanged,
  onClose,
}: ShowtimeRoomChangeContentProps) => {
  const flow = useRoomChangePlan(showtime.showtimeId);
  useBlockDialogClose(flow.isApplying);

  if (flow.result) {
    return (
      <RoomChangeResult
        movieTitle={showtime.movieTitle}
        result={flow.result}
        onClose={() => {
          onChanged?.();
          onClose();
        }}
      />
    );
  }

  const isBusy = flow.isPreviewing || flow.isApplying;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-0.5 cut-sm bg-secondary p-3 text-sm">
        <span className="font-medium text-foreground">{showtime.movieTitle}</span>
        <span className="text-muted-foreground tabular-nums">
          {formatDate(showtime.startTime)} · {formatTime(showtime.startTime)} -{" "}
          {formatTime(showtime.endTime)} · {showtime.roomName}
        </span>
      </div>

      <RoomChangeTargetPicker
        sourceRoomId={showtime.roomId}
        value={flow.targetRoomId}
        disabled={isBusy}
        onChange={flow.selectRoom}
      />

      {flow.previewError !== null && (
        <div className="flex flex-col gap-2">
          <p role="alert" className="text-sm text-destructive">
            {getDisplayMessage(flow.previewError, { preferServerMessage: true })}
          </p>
          {flow.plan === null && (
            <Button variant="outline" size="sm" className="w-fit" onClick={flow.refreshPreview}>
              Thử lại
            </Button>
          )}
        </div>
      )}

      {flow.plan === null && flow.isPreviewing && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      )}

      {flow.plan !== null && (
        <>
          <RoomChangeSummary plan={flow.plan} />
          <RoomChangeMappingTable
            plan={flow.plan}
            overrides={flow.overrides}
            isBusy={isBusy}
            onAssign={flow.assignSeat}
            onClearOverride={flow.clearOverride}
          />
          <div className="flex flex-col gap-1.5">
            <label htmlFor="room-change-note" className="text-sm font-medium text-foreground">
              Ghi chú (không bắt buộc)
            </label>
            <Textarea
              id="room-change-note"
              value={flow.note}
              maxLength={ROOM_CHANGE_NOTE_MAX_LENGTH}
              rows={2}
              placeholder="Ví dụ: Phòng 1 thay máy chiếu"
              disabled={flow.isApplying}
              onChange={(event) => flow.setNote(event.target.value)}
            />
          </div>
        </>
      )}

      <div className="sticky bottom-0 z-10 flex justify-end gap-2 border-t border-border bg-(--modal-surface,var(--background)) py-3">
        <Button variant="outline" onClick={onClose} disabled={flow.isApplying}>
          Đóng
        </Button>
        <Button onClick={flow.apply} disabled={!flow.canApply}>
          <ArrowRightLeft className="size-4" />
          <span>{flow.isApplying ? "Đang chuyển..." : "Xác nhận chuyển phòng"}</span>
        </Button>
      </div>
    </div>
  );
};
