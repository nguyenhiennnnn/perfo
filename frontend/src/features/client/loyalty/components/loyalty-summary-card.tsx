import { Clock } from "lucide-react";
import { MembershipTierBadge } from "@/components/common/status-badge";
import { PERCENT_MAX } from "@/config/default-constants";
import { formatDateTime, formatPoints } from "@/lib/formatters";
import type { LoyaltySummaryDto } from "../types";

interface LoyaltySummaryCardProps {
  summary: LoyaltySummaryDto;
}

export const LoyaltySummaryCard = ({ summary }: LoyaltySummaryCardProps) => {
  const { rankPoints, pointsToNextTier } = summary;
  const progress =
    pointsToNextTier != null && rankPoints + pointsToNextTier > 0
      ? Math.min(PERCENT_MAX, (rankPoints / (rankPoints + pointsToNextTier)) * PERCENT_MAX)
      : PERCENT_MAX;

  return (
    <section aria-label="Điểm thưởng của bạn" className="cut-md bg-card p-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs text-muted-foreground">Số dư điểm đổi quà</p>
          <p className="mt-1 text-3xl font-bold tabular-nums text-accent-text">
            {formatPoints(summary.balance)}
          </p>
        </div>
        <div>
          <p className="text-xs text-muted-foreground">Hạng thành viên</p>
          <div className="mt-1">
            <MembershipTierBadge tierName={summary.tierName ?? "Thành viên"} />
          </div>
        </div>
        <div className="text-right">
          <p className="text-xs text-muted-foreground">Điểm xếp hạng</p>
          <p className="mt-1 text-lg font-bold tabular-nums text-foreground">
            {formatPoints(rankPoints)}
          </p>
        </div>
      </div>

      {summary.nextTierName && pointsToNextTier != null && (
        <div className="mt-4">
          <div className="h-2 w-full overflow-hidden bg-muted">
            <div className="h-full bg-primary" style={{ width: `${progress}%` }} />
          </div>
          <p className="mt-1.5 text-xs text-muted-foreground">
            Cần thêm {formatPoints(pointsToNextTier)} điểm xếp hạng để đạt hạng {summary.nextTierName}
          </p>
        </div>
      )}

      {summary.debt > 0 && (
        <p className="mt-4 text-sm text-muted-foreground">
          Bạn đang có {formatPoints(summary.debt)} điểm cần hoàn lại do đơn bị hủy hoặc hoàn tiền;
          số này sẽ được trừ vào lần tích điểm tiếp theo.
        </p>
      )}

      {summary.expiringPoints > 0 && (
        <div role="status" className="mt-4 bg-warning/10 p-3">
          <p className="flex items-center gap-2 text-sm font-medium text-warning-text">
            <Clock className="size-4 shrink-0" aria-hidden />
            {formatPoints(summary.expiringPoints)} điểm sẽ hết hạn trong {summary.expiringWithinDays}{" "}
            ngày tới
          </p>
          <ul className="mt-2 space-y-1 text-xs text-muted-foreground">
            {summary.expiringLots.map((lot) => (
              <li key={lot.entryId} className="flex justify-between gap-3">
                <span className="tabular-nums">{formatPoints(lot.points)} điểm</span>
                <span>Hết hạn {formatDateTime(lot.expiresAt)}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      <p className="mt-4 border-t border-border pt-3 text-xs text-muted-foreground">
        Điểm đổi quà được tính theo từng lô, mỗi lô có hạn dùng riêng và được dùng theo thứ tự lô cũ
        trước. Đổi quà chỉ trừ điểm đổi quà, không làm thay đổi hạng.
      </p>
    </section>
  );
};
