import { formatCurrency } from "@/lib/formatters";
import type { OrderDetailDto } from "../../types";
import type { PaymentResultState } from "../../utils/order-payment";
import { BookingHoldTimer } from "../booking/booking-hold-timer";
import { BookingPaymentTotals } from "../booking/booking-order-summary";

interface BookingResultPaymentProps {
  order: OrderDetailDto;
  state: PaymentResultState;
}

export const BookingResultPayment = ({ order, state }: BookingResultPaymentProps) => {
  const isPayable = state === "outstanding" || state === "failed";
  const hasPaidExpired = state === "expired" && order.payment.netPaidAmount > 0;

  if (!isPayable && !hasPaidExpired) return null;

  return (
    <div className="flex w-full flex-col gap-4">
      {isPayable && order.expiresAt && <BookingHoldTimer expiresAt={order.expiresAt} />}
      <div className="bg-card p-4">
        <BookingPaymentTotals order={order} />
      </div>
      {hasPaidExpired && (
        <p className="text-sm text-muted-foreground">
          Đơn đã nhận {formatCurrency(order.payment.netPaidAmount)} trước khi hết hạn. Liên hệ rạp để được hỗ trợ hoàn
          tiền.
        </p>
      )}
    </div>
  );
};
