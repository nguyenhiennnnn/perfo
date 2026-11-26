import { Hourglass } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { WaitlistStatus } from "@/types/enum";
import { useIsAuthenticated } from "@/stores/auth-store";
import { useMyWaitlistEntry, useWaitlistNoticeToast } from "../hooks/use-waitlist";
import { useWaitlistActions } from "../hooks/use-waitlist-actions";
import { isWaitlistActive } from "../utils/waitlist-status";
import { WaitlistPriorityCard } from "./waitlist-priority-card";

interface WaitlistPanelProps {
  showtimeId: number;
  soldOut: boolean;
}

export const WaitlistPanel = ({ showtimeId, soldOut }: WaitlistPanelProps) => {
  const isAuthenticated = useIsAuthenticated();
  const query = useMyWaitlistEntry(showtimeId, isAuthenticated);
  const actions = useWaitlistActions(showtimeId);
  const entry = query.data;
  useWaitlistNoticeToast(entry);

  if (isAuthenticated && query.isLoading) {
    return soldOut ? <Skeleton className="h-24 w-full cut-md" /> : null;
  }

  if (isWaitlistActive(entry)) {
    if (entry.status === WaitlistStatus.Notified) {
      return (
        <WaitlistPriorityCard
          entry={entry}
          isWithdrawing={actions.isWithdrawing}
          onWithdraw={actions.confirmWithdraw}
        />
      );
    }

    if (entry.status === WaitlistStatus.Held) {
      return (
        <section className="cut-md bg-card p-4 text-sm text-muted-foreground">
          Bạn đã giữ ghế cho suất này từ danh sách chờ.
        </section>
      );
    }

    return (
      <section
        aria-label="Trạng thái đăng ký chờ"
        className="flex flex-col gap-3 cut-md bg-card p-4 sm:flex-row sm:items-center sm:justify-between"
      >
        <div className="min-w-0">
          <h3 className="t-h3 flex items-center gap-2 text-foreground">
            <Hourglass className="size-5 shrink-0 text-warning-text" aria-hidden />
            Đang trong danh sách chờ
          </h3>
          <p className="mt-1 text-sm text-muted-foreground">
            {entry.seatCount} ghế{entry.requireAdjacent ? " liền kề" : ""}
            {entry.queuePosition ? `, vị trí số ${entry.queuePosition}` : ""}. Chúng tôi sẽ báo ngay khi có ghế.
          </p>
        </div>
        <Button variant="outline" onClick={actions.confirmWithdraw} disabled={actions.isWithdrawing}>
          Rút đăng ký
        </Button>
      </section>
    );
  }

  if (!soldOut) return null;

  return (
    <section
      aria-label="Đăng ký chờ ghế"
      className="flex flex-col gap-3 cut-md bg-card p-4 sm:flex-row sm:items-center sm:justify-between"
    >
      <div className="min-w-0">
        <h3 className="t-h3 text-foreground">Suất chiếu đã hết ghế</h3>
        <p className="mt-1 text-sm text-muted-foreground">
          Đăng ký chờ để được báo và ưu tiên giữ ghế khi có người nhả ghế.
        </p>
      </div>
      <Button onClick={actions.openRegister}>Đăng ký chờ</Button>
    </section>
  );
};
