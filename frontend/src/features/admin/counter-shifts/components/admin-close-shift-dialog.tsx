import { useEffect, useState } from "react";
import { DoorClosed } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { useDialogClose, useDialogDismissGuard } from "@/stores/dialog-store";
import {
  COUNTER_SHIFT_MAX_CASH,
  COUNTER_SHIFT_NOTE_MAX_LENGTH,
} from "@/features/admin/counter/types/counter-shift";
import { parseMoneyInput } from "@/features/admin/counter/utils/cash";
import { useAdminCloseCounterShift } from "../hooks/use-admin-counter-shifts";
import type { CounterShift } from "../types";

interface AdminCloseShiftDialogContentProps {
  shift: CounterShift;
}

export const AdminCloseShiftDialogContent = ({ shift }: AdminCloseShiftDialogContentProps) => {
  const closeDialog = useDialogClose();
  const { block, release } = useDialogDismissGuard();
  const closeShift = useAdminCloseCounterShift();

  const [countedCash, setCountedCash] = useState(0);
  const [note, setNote] = useState("");

  useEffect(() => {
    if (!closeShift.isPending) return;
    block();
    return release;
  }, [closeShift.isPending, block, release]);

  const trimmedNote = note.trim();
  const isCashInvalid = countedCash < 0 || countedCash > COUNTER_SHIFT_MAX_CASH;
  const isNoteMissing = trimmedNote.length === 0;
  const isNoteTooLong = note.length > COUNTER_SHIFT_NOTE_MAX_LENGTH;
  const canSubmit = !isCashInvalid && !isNoteMissing && !isNoteTooLong && !closeShift.isPending;

  const handleSubmit = async (event: React.SyntheticEvent) => {
    event.preventDefault();
    if (!canSubmit) return;
    try {
      await closeShift.mutateAsync({
        id: shift.counterShiftId,
        countedCash,
        note: trimmedNote,
      });
      closeDialog();
    } catch {
      return;
    }
  };

  return (
    <form onSubmit={(e) => void handleSubmit(e)} className="flex flex-col gap-4">
      <p className="text-sm text-muted-foreground">
        Ca của <span className="font-medium text-foreground">{shift.employeeName}</span>, mở lúc{" "}
        {formatDateTime(shift.openedAt)}, tiền đầu ca{" "}
        <span className="font-medium tabular-nums text-foreground">
          {formatCurrency(shift.openingCash)}
        </span>
        . Nhập số tiền mặt thực đếm trong két và lý do đóng thay.
      </p>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="admin-close-counted-cash">Tiền mặt thực đếm</Label>
        <Input
          id="admin-close-counted-cash"
          type="number"
          inputMode="numeric"
          min={0}
          max={COUNTER_SHIFT_MAX_CASH}
          step={1000}
          value={countedCash}
          autoFocus
          aria-invalid={isCashInvalid || undefined}
          onChange={(e) => setCountedCash(parseMoneyInput(e.target.value))}
        />
        <p className={cn("text-xs", isCashInvalid ? "text-destructive" : "text-muted-foreground")}>
          {isCashInvalid
            ? `Số tiền không được âm và không vượt quá ${formatCurrency(COUNTER_SHIFT_MAX_CASH)}.`
            : `Hiện nhập: ${formatCurrency(countedCash)}`}
        </p>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="admin-close-note">Ghi chú (bắt buộc)</Label>
        <Textarea
          id="admin-close-note"
          rows={3}
          className="min-h-20 resize-none"
          value={note}
          placeholder="Ví dụ: nhân viên quên đóng ca, quản trị đếm két thay"
          aria-invalid={isNoteTooLong || undefined}
          onChange={(e) => setNote(e.target.value)}
        />
        <p className={cn("text-xs", isNoteTooLong ? "text-destructive" : "text-muted-foreground")}>
          {note.length}/{COUNTER_SHIFT_NOTE_MAX_LENGTH}
        </p>
      </div>

      <div className="grid grid-cols-2 gap-2">
        <Button type="button" variant="outline" onClick={closeDialog} disabled={closeShift.isPending}>
          Quay lại
        </Button>
        <Button type="submit" disabled={!canSubmit}>
          <DoorClosed className="size-4" />
          <span>{closeShift.isPending ? FORM_PROCESSING_LABEL : "Đóng thay ca"}</span>
        </Button>
      </div>
    </form>
  );
};
