import { useEffect, useState } from "react";
import { CheckCircle2, DoorClosed } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { useDialogClose, useDialogDismissGuard } from "@/stores/dialog-store";
import { useCloseCounterShift } from "../hooks/use-counter-shift";
import {
  COUNTER_SHIFT_MAX_CASH,
  COUNTER_SHIFT_NOTE_MAX_LENGTH,
  type CounterShift,
} from "../types/counter-shift";
import { parseMoneyInput } from "../utils/cash";

const describeVariance = (variance: number) => {
  if (variance === 0) return "Khớp sổ sách";
  return variance > 0 ? "Thừa tiền so với sổ sách" : "Thiếu tiền so với sổ sách";
};

interface CounterShiftCloseDialogContentProps {
  shift: CounterShift;
}

export const CounterShiftCloseDialogContent = ({
  shift,
}: CounterShiftCloseDialogContentProps) => {
  const closeDialog = useDialogClose();
  const { block, release } = useDialogDismissGuard();
  const closeShift = useCloseCounterShift();

  const [countedCash, setCountedCash] = useState(0);
  const [note, setNote] = useState("");
  const [result, setResult] = useState<CounterShift | null>(null);

  useEffect(() => {
    if (!closeShift.isPending) return;
    block();
    return release;
  }, [closeShift.isPending, block, release]);

  const isCashInvalid = countedCash < 0 || countedCash > COUNTER_SHIFT_MAX_CASH;
  const isNoteTooLong = note.length > COUNTER_SHIFT_NOTE_MAX_LENGTH;
  const canSubmit = !isCashInvalid && !isNoteTooLong && !closeShift.isPending;

  const handleSubmit = async (event: React.SyntheticEvent) => {
    event.preventDefault();
    if (!canSubmit) return;
    try {
      const res = await closeShift.mutateAsync({
        id: shift.counterShiftId,
        countedCash,
        note: note.trim() || null,
      });
      setResult(res.data);
    } catch {
      return;
    }
  };

  if (result) {
    const variance = result.variance ?? 0;
    return (
      <div className="flex flex-col gap-4">
        <div className="flex items-center gap-2 text-sm font-medium text-foreground">
          <CheckCircle2 className="size-5 text-success" />
          <span>Đã đóng ca lúc {formatDateTime(result.closedAt)}</span>
        </div>
        <dl className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-2 bg-card p-4 text-sm">
          <dt className="text-muted-foreground">Tiền đầu ca</dt>
          <dd className="text-right font-medium tabular-nums">
            {formatCurrency(result.openingCash)}
          </dd>
          <dt className="text-muted-foreground">Phải có theo sổ sách</dt>
          <dd className="text-right font-medium tabular-nums">
            {formatCurrency(result.expectedCash)}
          </dd>
          <dt className="text-muted-foreground">Thực đếm</dt>
          <dd className="text-right font-medium tabular-nums">
            {formatCurrency(result.countedCash)}
          </dd>
          <dt className="font-medium text-foreground">Chênh lệch</dt>
          <dd
            data-testid="counter-shift-variance"
            className={cn(
              "text-right font-bold tabular-nums",
              variance === 0 ? "text-success" : "text-warning-text",
            )}
          >
            {formatCurrency(variance)}
          </dd>
        </dl>
        <p
          role="status"
          className={cn(
            "p-3 text-sm text-foreground",
            variance === 0 ? "bg-success/10" : "bg-warning/10",
          )}
        >
          {describeVariance(variance)}.
          {result.note ? ` Ghi chú: ${result.note}` : ""}
        </p>
        <Button onClick={closeDialog}>Xong</Button>
      </div>
    );
  }

  return (
    <form onSubmit={(e) => void handleSubmit(e)} className="flex flex-col gap-4">
      <p className="text-sm text-muted-foreground">
        Đếm toàn bộ tiền mặt trong két rồi nhập vào đây. Hệ thống sẽ đối chiếu với sổ sách và ghi
        lại chênh lệch.
      </p>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="counter-counted-cash">Tiền mặt thực đếm</Label>
        <Input
          id="counter-counted-cash"
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
        {isCashInvalid ? (
          <p className="text-xs text-destructive">
            Số tiền thực đếm không được âm và không vượt quá{" "}
            {formatCurrency(COUNTER_SHIFT_MAX_CASH)}.
          </p>
        ) : (
          <p className="text-xs text-muted-foreground">
            Tiền đầu ca {formatCurrency(shift.openingCash)}. Hiện nhập: {formatCurrency(countedCash)}
          </p>
        )}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="counter-close-note">Ghi chú (nếu có chênh lệch)</Label>
        <Textarea
          id="counter-close-note"
          rows={3}
          className="min-h-20 resize-none"
          value={note}
          placeholder="Ví dụ: thối nhầm tiền cho khách"
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
          <span>{closeShift.isPending ? FORM_PROCESSING_LABEL : "Đóng ca"}</span>
        </Button>
      </div>
    </form>
  );
};
