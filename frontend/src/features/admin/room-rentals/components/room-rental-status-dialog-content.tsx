import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Field, FieldDescription, FieldLabel } from "@/components/ui/field";
import { Textarea } from "@/components/ui/textarea";
import { RoomRentalRequestStatus } from "@/types/enum";
import { useChangeRoomRentalStatus } from "../hooks/use-admin-room-rentals";
import {
  ROOM_RENTAL_ADMIN_STATUS,
  ROOM_RENTAL_NOTE_MAX_LENGTH,
} from "../utils/room-rental-admin-labels";

interface RoomRentalStatusDialogContentProps {
  requestId: number;
  target: RoomRentalRequestStatus;
  onDone: () => void;
}

export const RoomRentalStatusDialogContent = ({
  requestId,
  target,
  onDone,
}: RoomRentalStatusDialogContentProps) => {
  const [note, setNote] = useState("");
  const changeStatus = useChangeRoomRentalStatus(requestId);
  const isCancel = target === RoomRentalRequestStatus.Cancelled;

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Chuyển yêu cầu sang trạng thái <strong>{ROOM_RENTAL_ADMIN_STATUS[target].label}</strong>.
        {isCancel && " Khách sẽ không thể chấp nhận hoặc thanh toán báo giá cũ nữa."}
      </p>
      <Field>
        <FieldLabel htmlFor="room-rental-status-note">Ghi chú (không bắt buộc)</FieldLabel>
        <Textarea
          id="room-rental-status-note"
          value={note}
          rows={3}
          maxLength={ROOM_RENTAL_NOTE_MAX_LENGTH}
          className="min-h-20 resize-none"
          onChange={(event) => setNote(event.target.value)}
          disabled={changeStatus.isPending}
        />
        <FieldDescription>Ghi chú được lưu vào nhật ký thao tác.</FieldDescription>
      </Field>
      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onDone} disabled={changeStatus.isPending}>
          Đóng
        </Button>
        <Button
          variant={isCancel ? "destructive" : "default"}
          disabled={changeStatus.isPending}
          onClick={() =>
            changeStatus.mutate(
              { status: target, note: note.trim() || null },
              { onSuccess: onDone },
            )
          }
        >
          {changeStatus.isPending ? "Đang lưu..." : "Xác nhận"}
        </Button>
      </div>
    </div>
  );
};
