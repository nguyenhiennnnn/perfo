import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Textarea } from "@/components/ui/textarea";
import { ROOM_RENTAL_DECLINE_REASON_MAX_LENGTH } from "../constants";

interface RoomRentalDeclineDialogProps {
  open: boolean;
  versionNumber: number;
  isPending: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmit: (reason: string | null) => void;
}

export const RoomRentalDeclineDialog = ({
  open,
  versionNumber,
  isPending,
  onOpenChange,
  onSubmit,
}: RoomRentalDeclineDialogProps) => {
  const [reason, setReason] = useState("");

  return (
    <Dialog open={open} onOpenChange={(next) => !isPending && onOpenChange(next)}>
      <DialogContent size="md">
        <DialogHeader>
          <DialogTitle>Từ chối báo giá?</DialogTitle>
          <DialogDescription>
            Bạn đang từ chối báo giá phiên bản {versionNumber}. Cho rạp biết lý do để điều chỉnh
            báo giá phù hợp hơn (không bắt buộc).
          </DialogDescription>
        </DialogHeader>
        <Textarea
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          maxLength={ROOM_RENTAL_DECLINE_REASON_MAX_LENGTH}
          placeholder="Ví dụ: ngân sách cao hơn dự kiến, cần đổi giờ chiếu..."
          aria-label="Lý do từ chối báo giá"
          disabled={isPending}
          className="min-h-24"
        />
        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            disabled={isPending}
            onClick={() => onOpenChange(false)}
          >
            Quay lại
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={isPending}
            onClick={() => onSubmit(reason.trim() || null)}
          >
            {isPending ? "Đang gửi..." : "Từ chối báo giá"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
