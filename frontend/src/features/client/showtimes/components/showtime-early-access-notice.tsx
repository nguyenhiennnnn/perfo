import { Lock, Sparkles } from "lucide-react";

import { formatDateTime } from "@/lib/formatters";

import { useEarlyAccessGate } from "../hooks/use-early-access-gate";
import type { ShowtimeEarlyAccessDto } from "../types";
import { describeEarlyAccessGroups, formatSaleCountdown } from "../utils/early-access";

interface ShowtimeEarlyAccessNoticeProps {
  earlyAccess: ShowtimeEarlyAccessDto;
}

export const ShowtimeEarlyAccessNotice = ({ earlyAccess }: ShowtimeEarlyAccessNoticeProps) => {
  const { isRestricted, isBlocked, remainingMs } = useEarlyAccessGate(earlyAccess);
  if (!isRestricted) return null;

  const groups = describeEarlyAccessGroups(earlyAccess);
  const Icon = isBlocked ? Lock : Sparkles;

  return (
    <div
      role="status"
      className="flex flex-col gap-2 bg-warning/10 p-4 text-sm sm:flex-row sm:items-start sm:justify-between"
    >
      <div className="flex min-w-0 items-start gap-3">
        <Icon className="mt-0.5 size-5 shrink-0 text-warning-text" aria-hidden />
        <div className="min-w-0 space-y-1">
          <p className="font-medium text-foreground">
            {isBlocked
              ? "Bạn chưa đủ điều kiện mua vé suất sự kiện này lúc này"
              : "Bạn đang được mở bán sớm suất sự kiện này"}
          </p>
          {isBlocked && earlyAccess.ineligibleReason && (
            <p className="text-muted-foreground">{earlyAccess.ineligibleReason}</p>
          )}
          {groups.length > 0 && (
            <p className="text-muted-foreground">Đối tượng mở bán sớm: {groups.join("; ")}.</p>
          )}
          <p className="text-muted-foreground">
            Mở bán chung lúc {formatDateTime(earlyAccess.generalSaleStartTime)}.
          </p>
        </div>
      </div>
      <p className="shrink-0 text-muted-foreground">
        Mở bán chung sau{" "}
        <span className="t-code tabular-nums text-foreground">{formatSaleCountdown(remainingMs)}</span>
      </p>
    </div>
  );
};
