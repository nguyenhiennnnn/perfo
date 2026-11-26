import { WaitlistStatus } from "@/types/enum";
import { useIsAuthenticated } from "@/stores/auth-store";
import { useMyWaitlistEntry, useWaitlistNoticeToast } from "../hooks/use-waitlist";
import { useWaitlistActions } from "../hooks/use-waitlist-actions";
import { useWaitlistHold } from "../hooks/use-waitlist-hold";
import { useWaitlistPriorityTimer } from "../hooks/use-waitlist-priority-timer";
import type { WaitlistEntryDto } from "../types";
import { getReservedSeatIds, isWaitlistActive } from "../utils/waitlist-status";

const ROW_BUTTON_CLASS =
  "h-11 cursor-pointer cut-sm bg-primary px-5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50";

const RowNotified = ({ entry }: { entry: WaitlistEntryDto }) => {
  const { label, expired } = useWaitlistPriorityTimer(entry);
  const hold = useWaitlistHold(entry.showtimeId, getReservedSeatIds(entry));

  return (
    <div className="flex flex-col items-center gap-1">
      <p role="timer" className="text-xs text-secondary-foreground">
        Ưu tiên còn <span className="t-code font-bold tabular-nums">{label}</span>
      </p>
      <button
        type="button"
        className={ROW_BUTTON_CLASS}
        disabled={expired || !hold.isReady || hold.isHolding}
        onClick={hold.holdSeats}
      >
        {hold.isHolding ? "Đang giữ..." : "Giữ ghế"}
      </button>
    </div>
  );
};

export const WaitlistRowAction = ({
  showtimeId,
  enabled,
}: {
  showtimeId: number;
  enabled: boolean;
}) => {
  const isAuthenticated = useIsAuthenticated();
  const query = useMyWaitlistEntry(showtimeId, enabled && isAuthenticated);
  const actions = useWaitlistActions(showtimeId);
  const entry = query.data;
  useWaitlistNoticeToast(entry);

  if (!enabled) return <p className="t-h3">Hết ghế</p>;

  if (isWaitlistActive(entry)) {
    if (entry.status === WaitlistStatus.Notified) return <RowNotified entry={entry} />;

    if (entry.status === WaitlistStatus.Held) {
      return <p className="text-sm font-medium">Đã giữ ghế</p>;
    }

    return (
      <div className="flex flex-col items-center gap-1">
        <p className="text-sm font-medium">
          Đang chờ{entry.queuePosition ? `, số ${entry.queuePosition}` : ""}
        </p>
        <button
          type="button"
          disabled={actions.isWithdrawing}
          onClick={actions.confirmWithdraw}
          className="cursor-pointer text-xs underline underline-offset-4 hover:no-underline disabled:cursor-not-allowed disabled:opacity-50"
        >
          Rút đăng ký
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center gap-1">
      <p className="text-sm font-medium">Hết ghế</p>
      <button
        type="button"
        disabled={isAuthenticated && query.isLoading}
        onClick={actions.openRegister}
        className={ROW_BUTTON_CLASS}
      >
        Đăng ký chờ
      </button>
    </div>
  );
};
