import { BellRing } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useWaitlistHold } from "../hooks/use-waitlist-hold";
import { useWaitlistPriorityTimer } from "../hooks/use-waitlist-priority-timer";
import type { WaitlistEntryDto } from "../types";
import { getReservedSeatIds } from "../utils/waitlist-status";

interface WaitlistPriorityCardProps {
  entry: WaitlistEntryDto;
  isWithdrawing: boolean;
  onWithdraw: () => void;
}

export const WaitlistPriorityCard = ({ entry, isWithdrawing, onWithdraw }: WaitlistPriorityCardProps) => {
  const { label, expired } = useWaitlistPriorityTimer(entry);
  const hold = useWaitlistHold(entry.showtimeId, getReservedSeatIds(entry));

  return (
    <section
      aria-labelledby={`waitlist-priority-${entry.showtimeId}`}
      className="cut-md border border-success/40 bg-success/10 p-4"
    >
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0">
          <h3
            id={`waitlist-priority-${entry.showtimeId}`}
            className="t-h3 flex items-center gap-2 text-foreground"
          >
            <BellRing className="size-5 shrink-0 text-success" aria-hidden />
            Đã có ghế cho bạn
          </h3>
          <p className="mt-1 text-sm text-muted-foreground">
            {entry.seatCount} ghế{entry.requireAdjacent ? " liền kề" : ""}
            {hold.seatLabels.length > 0 && `: ${hold.seatLabels.join(", ")}`}.{" "}
            {expired
              ? "Đã hết thời gian ưu tiên, ghế sẽ mở cho người kế tiếp."
              : "Giữ ghế trước khi hết giờ, sau đó ghế sẽ mở cho người kế tiếp."}
          </p>
        </div>

        <div className="flex items-center justify-between gap-4 sm:justify-end">
          <div role="timer" className="flex flex-col items-start sm:items-end">
            <span className="text-xs text-muted-foreground">Ưu tiên còn</span>
            <span className="t-code text-2xl font-bold tabular-nums text-success">{label}</span>
          </div>
          <Button onClick={hold.holdSeats} disabled={expired || !hold.isReady || hold.isHolding}>
            {hold.isHolding ? "Đang giữ ghế..." : "Giữ ghế"}
          </Button>
        </div>
      </div>

      <div className="mt-3 border-t border-border pt-2">
        <Button variant="link" size="sm" className="px-0" onClick={onWithdraw} disabled={isWithdrawing}>
          Rút đăng ký chờ
        </Button>
      </div>
    </section>
  );
};
