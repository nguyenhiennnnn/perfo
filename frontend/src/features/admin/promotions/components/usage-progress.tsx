import { cn } from "@/lib/utils";
import { formatNumber } from "@/lib/formatters";

import { getUsageLevel, ratioToPercent, type UsageLevel } from "../utils/promotion-admin-labels";

interface UsageProgressProps {
  label?: string;
  used: number;
  limit?: number | null;
  ratio?: number | null;
  format?: (value: number) => string;
  className?: string;
}

const FILL_CLASS: Record<UsageLevel, string> = {
  normal: "bg-success",
  warning: "bg-warning",
  exhausted: "bg-destructive",
};

const TEXT_CLASS: Record<UsageLevel, string> = {
  normal: "text-muted-foreground",
  warning: "text-warning-text",
  exhausted: "text-destructive",
};

export const UsageProgress = ({
  label,
  used,
  limit,
  ratio,
  format = formatNumber,
  className,
}: UsageProgressProps) => {
  const percent = ratioToPercent(ratio);
  const level = getUsageLevel(ratio);
  const width = percent === null ? 0 : Math.min(percent, 100);

  return (
    <div className={cn("min-w-32 space-y-1", className)}>
      {label && <p className="text-xs font-medium text-foreground">{label}</p>}
      {percent === null || limit == null ? (
        <p className="text-xs tabular-nums text-muted-foreground">
          {format(used)} · Không giới hạn
        </p>
      ) : (
        <>
          <div
            role="progressbar"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={Math.min(percent, 100)}
            className="h-1.5 w-full overflow-hidden bg-muted"
          >
            <div className={cn("h-full", FILL_CLASS[level])} style={{ width: `${width}%` }} />
          </div>
          <p className={cn("text-xs tabular-nums", TEXT_CLASS[level])}>
            {format(used)}/{format(limit)} · {percent}%
          </p>
        </>
      )}
    </div>
  );
};
