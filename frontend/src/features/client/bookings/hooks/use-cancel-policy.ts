import { DEFAULT_CANCEL_DEADLINE_HOURS, MS_PER_HOUR, PERCENT_MAX } from "@/config/default-constants";
import { useCinemaSettings } from "@/features/client/settings/hooks/use-settings";
import { useNow } from "@/hooks/use-now";
import { BookingStatus } from "@/types/enum";

export type CancelBlockReason = "notConfirmed" | "nonRefundable" | "deadlinePassed";

export function useCancelPolicy(status: BookingStatus, startTime: string, finalAmount: number) {
  const now = useNow();
  const { data: settings } = useCinemaSettings();

  const deadlineHours = settings?.cancelDeadlineHours ?? DEFAULT_CANCEL_DEADLINE_HOURS;
  const refundPercent = settings?.cancelRefundPercent ?? PERCENT_MAX;
  const refundAmount = Math.round((finalAmount * refundPercent) / PERCENT_MAX);

  let blockReason: CancelBlockReason | null = null;
  if (status !== BookingStatus.Confirmed) blockReason = "notConfirmed";
  else if (refundPercent <= 0) blockReason = "nonRefundable";
  else if (new Date(startTime).getTime() - now <= deadlineHours * MS_PER_HOUR) blockReason = "deadlinePassed";

  return {
    canCancel: blockReason === null,
    blockReason,
    deadlineHours,
    refundPercent,
    refundAmount,
  };
}
