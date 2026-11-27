import type { UseQueryResult } from "@tanstack/react-query";
import { AlertTriangle, Clock, type LucideIcon } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { useCountdown } from "@/hooks/use-countdown";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { canPayOutstanding } from "../../utils/order-payment";
import type { OrderDetailDto } from "../../types";
import type { PaymentMethod } from "@/types/enum";
import { BookingOrderSummary } from "./booking-order-summary";
import { BookingPaymentMethod } from "./booking-payment-method";
import { BookingPaymentSkeleton } from "./booking-payment-skeleton";

interface OrderHoldNoticeProps {
  expiresAt: string;
}

const OrderHoldNotice = ({ expiresAt }: OrderHoldNoticeProps) => {
  const { label, remainingMs } = useCountdown(expiresAt);
  const isExpired = remainingMs <= 0;
  const Icon = isExpired ? AlertTriangle : Clock;

  return (
    <div
      role="timer"
      className={cn(
        "flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-medium",
        isExpired ? "bg-destructive/10 text-destructive" : "border border-border text-warning-text",
      )}
    >
      <Icon className="size-4 shrink-0" aria-hidden />
      {isExpired ? (
        <span>Đơn đã hết thời gian chờ thanh toán.</span>
      ) : (
        <>
          <span>Đơn được giữ còn</span>
          <span className="t-code text-lg font-bold tabular-nums">{label}</span>
        </>
      )}
    </div>
  );
};

export interface StandaloneOrderPaymentStepProps {
  expiresAt: string;
  orderQuery: UseQueryResult<OrderDetailDto>;
  paymentMethod: PaymentMethod;
  onPaymentMethodChange: (value: PaymentMethod) => void;
  isSubmitting: boolean;
  onSubmit: () => void;
  onCancel: () => void;
  isCancelling: boolean;
}

interface StandaloneOrderPaymentStepViewProps extends StandaloneOrderPaymentStepProps {
  icon: LucideIcon;
  cancelLabel: string;
}

export const StandaloneOrderPaymentStep = ({
  expiresAt,
  orderQuery,
  paymentMethod,
  onPaymentMethodChange,
  isSubmitting,
  onSubmit,
  onCancel,
  isCancelling,
  icon,
  cancelLabel,
}: StandaloneOrderPaymentStepViewProps) => (
  <div className="mx-auto flex w-full max-w-lg flex-col gap-4">
    <OrderHoldNotice expiresAt={expiresAt} />

    <QueryState
      query={orderQuery}
      skeleton={<BookingPaymentSkeleton />}
      errorTitle="Không tải được chi tiết đơn"
    >
      {(order) =>
        canPayOutstanding(order) ? (
          <>
            <BookingOrderSummary order={order} />
            <BookingPaymentMethod value={paymentMethod} onChange={onPaymentMethodChange} />
            <Button disabled={isSubmitting} onClick={onSubmit}>
              {isSubmitting
                ? "Đang chuyển đến cổng thanh toán..."
                : `Thanh toán ${formatCurrency(order.payment.outstandingAmount)}`}
            </Button>
          </>
        ) : (
          <EmptyState
            icon={icon}
            title="Đơn không còn cần thanh toán"
            description="Đơn đã thanh toán, đã hủy hoặc đã hết hạn. Xem kết quả để biết trạng thái mới nhất."
            action={<ButtonLink to={ROUTES.booking.result(order.orderId)}>Xem kết quả đơn</ButtonLink>}
          />
        )
      }
    </QueryState>

    <Button variant="outline" disabled={isCancelling} onClick={onCancel}>
      {isCancelling ? "Đang hủy..." : cancelLabel}
    </Button>
  </div>
);
