import { TriangleAlert } from "lucide-react";

import { formatCurrency, formatNumber } from "@/lib/formatters";
import { cn } from "@/lib/utils";

import type { RoomChangePreviewResponse } from "../../types";

interface SummaryStatProps {
  label: string;
  value: string;
  testId: string;
  tone?: "default" | "warning";
}

const SummaryStat = ({ label, value, testId, tone = "default" }: SummaryStatProps) => (
  <div className="flex flex-col gap-1 cut-sm bg-secondary p-3">
    <p
      data-testid={testId}
      className={cn(
        "t-numeral text-2xl tabular-nums",
        tone === "warning" ? "text-warning-text" : "text-foreground",
      )}
    >
      {value}
    </p>
    <p className="text-xs text-muted-foreground">{label}</p>
  </div>
);

interface RoomChangeSummaryProps {
  plan: RoomChangePreviewResponse;
}

export const RoomChangeSummary = ({ plan }: RoomChangeSummaryProps) => (
  <div className="flex flex-col gap-3">
    <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
      <SummaryStat
        label="Ghế có khách"
        value={formatNumber(plan.occupiedSeatCount)}
        testId="room-change-occupied"
      />
      <SummaryStat
        label={`Ghế của ${plan.targetRoomName}`}
        value={formatNumber(plan.targetSeatCount)}
        testId="room-change-target-seats"
      />
      <SummaryStat
        label="Ghế cần chỉnh"
        value={formatNumber(plan.needsReviewCount)}
        testId="room-change-needs-review"
        tone={plan.needsReviewCount > 0 ? "warning" : "default"}
      />
      <SummaryStat
        label={
          plan.refundDueCount > 0
            ? `Hoàn chênh lệch (${formatNumber(plan.refundDueCount)} vé)`
            : "Hoàn chênh lệch"
        }
        value={formatCurrency(plan.refundDueTotal)}
        testId="room-change-refund-total"
        tone={plan.refundDueCount > 0 ? "warning" : "default"}
      />
    </div>

    {plan.issues.length > 0 && (
      <div
        role="alert"
        className="flex items-start gap-3 cut-sm border border-destructive/40 bg-destructive/10 p-3"
      >
        <TriangleAlert className="mt-0.5 size-4 shrink-0 text-destructive" />
        <ul className="flex flex-col gap-1 text-sm text-destructive">
          {plan.issues.map((issue) => (
            <li key={issue}>{issue}</li>
          ))}
        </ul>
      </div>
    )}
  </div>
);
