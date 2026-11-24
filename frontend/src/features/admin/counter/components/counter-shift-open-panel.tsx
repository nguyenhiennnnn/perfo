import { useState } from "react";
import { DoorOpen } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { formatCurrency } from "@/lib/formatters";
import { useOpenCounterShift } from "../hooks/use-counter-shift";
import { COUNTER_SHIFT_MAX_CASH } from "../types/counter-shift";
import { parseMoneyInput } from "../utils/cash";

const OPEN_STEPS = [
  "Đếm tiền mặt trong két và nhập vào ô bên dưới làm tiền đầu ca.",
  "Bấm Mở ca để bắt đầu bán vé, bán combo và nhận hoàn tiền mặt.",
  "Khi hết ca, vào lại màn này để đóng ca và nhập số tiền thực đếm.",
];

export const CounterShiftOpenPanel = () => {
  const openShift = useOpenCounterShift();
  const [openingCash, setOpeningCash] = useState(0);

  const isInvalid = openingCash < 0 || openingCash > COUNTER_SHIFT_MAX_CASH;

  const handleSubmit = async (event: React.SyntheticEvent) => {
    event.preventDefault();
    if (isInvalid) return;
    try {
      await openShift.mutateAsync({ openingCash });
    } catch {
      return;
    }
  };

  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-6 py-6">
      <div className="flex flex-col gap-2 text-center">
        <DoorOpen className="mx-auto size-10 text-muted-foreground" />
        <h1 className="t-h2 text-foreground">Mở ca quầy để bắt đầu bán hàng</h1>
        <p className="text-sm text-muted-foreground">
          Bạn cần mở ca trước khi bán vé, bán combo hoặc xác nhận hoàn tiền mặt.
        </p>
      </div>

      <ol className="flex list-decimal flex-col gap-1.5 bg-card p-4 pl-8 text-sm text-foreground">
        {OPEN_STEPS.map((step) => (
          <li key={step}>{step}</li>
        ))}
      </ol>

      <form onSubmit={(e) => void handleSubmit(e)} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="counter-opening-cash">Tiền đầu ca</Label>
          <Input
            id="counter-opening-cash"
            type="number"
            inputMode="numeric"
            min={0}
            max={COUNTER_SHIFT_MAX_CASH}
            step={1000}
            value={openingCash}
            autoFocus
            aria-invalid={isInvalid || undefined}
            onChange={(e) => setOpeningCash(parseMoneyInput(e.target.value))}
          />
          {isInvalid ? (
            <p className="text-xs text-destructive">
              Tiền đầu ca không được âm và không vượt quá {formatCurrency(COUNTER_SHIFT_MAX_CASH)}.
            </p>
          ) : (
            <p className="text-xs text-muted-foreground">
              Hiện nhập: {formatCurrency(openingCash)}
            </p>
          )}
        </div>

        <Button type="submit" disabled={isInvalid || openShift.isPending}>
          <DoorOpen className="size-4" />
          <span>{openShift.isPending ? FORM_PROCESSING_LABEL : "Mở ca"}</span>
        </Button>
      </form>
    </div>
  );
};
