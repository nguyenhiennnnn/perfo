import { DoorClosed } from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useDialogOpen } from "@/stores/dialog-store";
import type { CounterShift } from "../types/counter-shift";
import { CounterShiftCloseDialogContent } from "./counter-shift-close-dialog";

interface CounterShiftBarProps {
  shift: CounterShift;
  closeDisabled?: boolean;
}

export const CounterShiftBar = ({ shift, closeDisabled }: CounterShiftBarProps) => {
  const openDialog = useDialogOpen();

  const handleClose = () =>
    openDialog({
      title: "Đóng ca quầy",
      content: <CounterShiftCloseDialogContent shift={shift} />,
    });

  return (
    <div className="flex flex-col gap-2 bg-card p-3 sm:flex-row sm:items-center sm:justify-between">
      <p className="text-sm text-muted-foreground">
        Ca của <span className="font-medium text-foreground">{shift.employeeName}</span>, mở lúc{" "}
        {formatDateTime(shift.openedAt)}, tiền đầu ca{" "}
        <span className="font-medium tabular-nums text-foreground">
          {formatCurrency(shift.openingCash)}
        </span>
      </p>
      <Button variant="outline" size="sm" onClick={handleClose} disabled={closeDisabled}>
        <DoorClosed className="size-4" />
        <span>Đóng ca</span>
      </Button>
    </div>
  );
};
