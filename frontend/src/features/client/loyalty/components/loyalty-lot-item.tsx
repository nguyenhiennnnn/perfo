import { Link } from "react-router";
import { Badge } from "@/components/ui/badge";
import { PERCENT_MAX } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { formatDateTime, formatPoints } from "@/lib/formatters";
import type { LoyaltyLotDto } from "../types";
import { LOT_STATUS_LABELS, LOT_STATUS_VARIANTS, LOT_TYPE_LABELS } from "../utils/loyalty-labels";

interface LoyaltyLotItemProps {
  lot: LoyaltyLotDto;
}

export const LoyaltyLotItem = ({ lot }: LoyaltyLotItemProps) => {
  const remainingPercent =
    lot.points > 0 ? Math.min(PERCENT_MAX, (lot.remainingPoints / lot.points) * PERCENT_MAX) : 0;

  const usage = [
    lot.redeemedPoints > 0 && `Đã đổi quà: ${formatPoints(lot.redeemedPoints)}`,
    lot.expiredPoints > 0 && `Đã hết hạn: ${formatPoints(lot.expiredPoints)}`,
    lot.revokedPoints > 0 && `Thu hồi do hủy hoặc hoàn đơn: ${formatPoints(lot.revokedPoints)}`,
  ].filter(Boolean);

  return (
    <article className="cut-md bg-card p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="text-sm font-medium text-foreground">{LOT_TYPE_LABELS[lot.type]}</p>
          <p className="text-xs text-muted-foreground">
            Nhận lúc {formatDateTime(lot.earnedAt)}
            {lot.orderId != null && (
              <>
                {" · "}
                <Link
                  to={ROUTES.account.ticketDetail(lot.orderId)}
                  className="text-primary-text hover:underline"
                >
                  Đơn #{lot.orderId}
                </Link>
              </>
            )}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <span className="text-lg font-bold tabular-nums text-accent-text">
            +{formatPoints(lot.points)}
          </span>
          <Badge variant={LOT_STATUS_VARIANTS[lot.status]}>{LOT_STATUS_LABELS[lot.status]}</Badge>
        </div>
      </div>

      <div className="mt-3">
        <div
          role="progressbar"
          aria-label="Điểm còn lại của lô"
          aria-valuemin={0}
          aria-valuemax={lot.points}
          aria-valuenow={lot.remainingPoints}
          className="h-2 w-full overflow-hidden bg-muted"
        >
          <div className="h-full bg-primary" style={{ width: `${remainingPercent}%` }} />
        </div>
        <div className="mt-1.5 flex flex-wrap justify-between gap-x-4 gap-y-1 text-xs text-muted-foreground">
          <span>
            Còn lại{" "}
            <span className="font-semibold tabular-nums text-foreground">
              {formatPoints(lot.remainingPoints)}
            </span>{" "}
            / {formatPoints(lot.points)} điểm
          </span>
          <span>Hạn dùng: {formatDateTime(lot.expiresAt)}</span>
        </div>
      </div>

      {usage.length > 0 && (
        <p className="mt-2 text-xs text-muted-foreground">{usage.join(" · ")}</p>
      )}
    </article>
  );
};
