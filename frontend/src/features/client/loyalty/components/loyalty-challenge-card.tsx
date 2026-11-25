import { Trophy } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { PERCENT_MAX } from "@/config/default-constants";
import { getBusinessToday } from "@/lib/business-time";
import { formatDate, formatDateTime } from "@/lib/formatters";
import type { MyLoyaltyChallengeDto } from "../types";
import { describeChallengeCondition, describeChallengeReward } from "../utils/loyalty-labels";

type StatusBadge = { label: string; variant: "success" | "default" | "secondary" | "warning" };

const resolveStatus = (challenge: MyLoyaltyChallengeDto): StatusBadge => {
  if (challenge.isCompleted) {
    return challenge.isRewardGranted
      ? { label: "Đã nhận thưởng", variant: "success" }
      : { label: "Hoàn thành", variant: "warning" };
  }
  if (challenge.isRunning) return { label: "Đang diễn ra", variant: "default" };
  return challenge.startsOn > getBusinessToday()
    ? { label: "Sắp diễn ra", variant: "secondary" }
    : { label: "Đã kết thúc", variant: "secondary" };
};

interface LoyaltyChallengeCardProps {
  challenge: MyLoyaltyChallengeDto;
}

export const LoyaltyChallengeCard = ({ challenge }: LoyaltyChallengeCardProps) => {
  const status = resolveStatus(challenge);
  const current = Math.min(challenge.currentCount, challenge.targetCount);
  const percent =
    challenge.targetCount > 0 ? Math.min(PERCENT_MAX, (current / challenge.targetCount) * PERCENT_MAX) : 0;

  return (
    <article className="flex gap-4 cut-md bg-card p-4">
      <div className="flex size-11 shrink-0 items-center justify-center cut-xs bg-primary/10 text-primary-text">
        <Trophy className="size-5" aria-hidden />
      </div>

      <div className="min-w-0 flex-1 space-y-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <h3 className="t-h3 text-foreground">{challenge.name}</h3>
            <p className="text-xs text-muted-foreground">
              {formatDate(challenge.startsOn)} - {formatDate(challenge.endsOn)}
            </p>
          </div>
          <Badge variant={status.variant}>{status.label}</Badge>
        </div>

        {challenge.description && (
          <p className="text-sm text-muted-foreground">{challenge.description}</p>
        )}

        <div>
          <div className="mb-1.5 flex flex-wrap justify-between gap-x-4 text-sm">
            <span className="text-foreground">{describeChallengeCondition(challenge)}</span>
            <span className="font-semibold tabular-nums text-foreground">
              {current}/{challenge.targetCount}
            </span>
          </div>
          <div
            role="progressbar"
            aria-label={`Tiến độ thử thách ${challenge.name}`}
            aria-valuemin={0}
            aria-valuemax={challenge.targetCount}
            aria-valuenow={current}
            className="h-2 w-full overflow-hidden bg-muted"
          >
            <div className="h-full bg-primary" style={{ width: `${percent}%` }} />
          </div>
        </div>

        <div className="flex flex-wrap justify-between gap-x-4 gap-y-1 text-xs text-muted-foreground">
          <span className="font-medium text-accent-text">{describeChallengeReward(challenge)}</span>
          {challenge.completedAt && <span>Hoàn thành lúc {formatDateTime(challenge.completedAt)}</span>}
        </div>

        <p className="text-xs text-muted-foreground">
          Chỉ tính phim đã check-in sau khi suất chiếu kết thúc; vé đặt nhưng không đến xem không được
          tính.
        </p>
      </div>
    </article>
  );
};
