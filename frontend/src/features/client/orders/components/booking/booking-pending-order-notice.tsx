import { Clock } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { useCountdown } from "@/hooks/use-countdown";
import { ROUTES } from "@/config/routes";
import type { ShowtimeMyHoldDto } from "@/features/client/showtimes/types";

interface BookingPendingOrderNoticeProps {
  showtimeId: number;
  hold: ShowtimeMyHoldDto;
}

export const BookingPendingOrderNotice = ({ showtimeId, hold }: BookingPendingOrderNoticeProps) => {
  const { label, remainingMs } = useCountdown(hold.expiresAt);

  if (remainingMs <= 0) return null;

  return (
    <div
      role="status"
      className="flex flex-col gap-3 bg-warning/10 p-4 sm:flex-row sm:items-center sm:justify-between"
    >
      <div className="flex items-start gap-3">
        <Clock className="mt-0.5 size-5 shrink-0 text-warning-text" aria-hidden />
        <div className="min-w-0">
          <p className="text-sm font-medium text-foreground">
            Bạn đang giữ {hold.showtimeSeatIds.length} ghế cho đơn{" "}
            <span className="t-code">{hold.orderCode}</span>
          </p>
          <p className="text-xs text-muted-foreground">
            Còn <span className="t-code tabular-nums text-foreground">{label}</span> để hoàn tất thanh toán.
          </p>
        </div>
      </div>
      <ButtonLink to={`${ROUTES.booking.detail(showtimeId)}?resume=${hold.orderId}`}>
        Tiếp tục thanh toán
      </ButtonLink>
    </div>
  );
};
