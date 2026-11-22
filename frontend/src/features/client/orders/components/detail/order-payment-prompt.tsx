import { ButtonLink } from "@/components/common/button-link";
import { ROUTES } from "@/config/routes";
import { useCountdown } from "@/hooks/use-countdown";
import { formatCurrency } from "@/lib/formatters";
import type { OrderDetailDto } from "../../types";
import { getOrderShowing } from "../../utils/order-lines";
import { canPayOutstanding } from "../../utils/order-payment";

interface OrderPaymentPromptProps {
  order: OrderDetailDto;
}

export const OrderPaymentPrompt = ({ order }: OrderPaymentPromptProps) => {
  const countdown = useCountdown(order.expiresAt ?? null);

  if (!canPayOutstanding(order)) return null;

  const showtimeId = getOrderShowing(order)?.showtimeId;
  const isHoldActive = Boolean(order.expiresAt) && countdown.remainingMs > 0;

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 cut-md bg-warning/10 p-4">
      <div>
        <p className="text-sm font-medium text-foreground">
          {isHoldActive
            ? `Đơn còn thiếu ${formatCurrency(order.payment.outstandingAmount)}. Hoàn tất thanh toán trước khi hết hạn giữ chỗ.`
            : "Thời gian giữ chỗ đã hết, đơn sẽ không thanh toán tiếp được."}
        </p>
        {isHoldActive && (
          <p className="mt-0.5 text-xs font-semibold text-warning-text">
            Còn {countdown.label} để thanh toán
          </p>
        )}
      </div>
      {showtimeId && (
        <ButtonLink
          to={
            isHoldActive
              ? `${ROUTES.booking.detail(showtimeId)}?resume=${order.orderId}`
              : ROUTES.booking.detail(showtimeId)
          }
          variant={isHoldActive ? "default" : "outline"}
        >
          {isHoldActive ? "Thanh toán tiếp" : "Đặt vé mới"}
        </ButtonLink>
      )}
    </div>
  );
};
