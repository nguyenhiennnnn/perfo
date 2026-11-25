import { useState } from "react";
import type { UseQueryResult } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { Ticket } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { formatCurrency } from "@/lib/formatters";
import { OrderStatus, OrderType, PaymentMethod } from "@/types/enum";
import type { OrderGiftCardResponse } from "@/features/client/gift-cards/types";
import type { OrderDetailDto } from "@/features/client/orders/types";
import { canPayOutstanding } from "../../utils/order-payment";
import { BookingHoldTimer } from "./booking-hold-timer";
import { BookingGiftCardSection } from "./booking-gift-card-section";
import { BookingOrderSummary } from "./booking-order-summary";
import { BookingPaymentMethod } from "./booking-payment-method";
import { BookingPaymentSkeleton } from "./booking-payment-skeleton";
import { BookingPromotionSection } from "./booking-promotion-section";

const canUsePromotion = (order: OrderDetailDto) =>
  order.status === OrderStatus.Pending &&
  (order.type === OrderType.Ticket || order.type === OrderType.Combo) &&
  order.payment.netPaidAmount === 0;

const canUseGiftCard = (order: OrderDetailDto) =>
  order.status === OrderStatus.Pending && order.type !== OrderType.GiftCard;

interface BookingPaymentStepProps {
  expiresAt: string;
  orderQuery: UseQueryResult<OrderDetailDto>;
  paymentMethod: PaymentMethod;
  onPaymentMethodChange: (value: PaymentMethod) => void;
  isSubmitting: boolean;
  onSubmit: () => void;
  onCancel: () => void;
  isCancelling: boolean;
}

export const BookingPaymentStep = ({
  expiresAt,
  orderQuery,
  paymentMethod,
  onPaymentMethodChange,
  isSubmitting,
  onSubmit,
  onCancel,
  isCancelling,
}: BookingPaymentStepProps) => {
  const navigate = useNavigate();
  const [giftCover, setGiftCover] = useState<OrderGiftCardResponse | null>(null);
  const order = orderQuery.data;
  const hasGiftCards = (giftCover?.cards.length ?? 0) > 0;
  const showCancel = !order || order.status === OrderStatus.Pending;

  return (
    <div className="mx-auto flex w-full max-w-lg flex-col gap-4">
      <BookingHoldTimer expiresAt={expiresAt} className="sticky top-16 z-40" />

      <QueryState
        query={orderQuery}
        skeleton={<BookingPaymentSkeleton />}
        errorTitle="Không tải được chi tiết đơn"
      >
        {(loadedOrder) =>
          canPayOutstanding(loadedOrder) ? (
            <>
              {loadedOrder.payment.netPaidAmount > 0 && (
                <p role="status" className="bg-warning/10 p-4 text-sm font-medium text-foreground">
                  Đơn đã trả {formatCurrency(loadedOrder.payment.netPaidAmount)}. Thanh toán phần còn lại trước khi
                  hết hạn giữ ghế để nhận vé.
                </p>
              )}
              <BookingOrderSummary order={loadedOrder} />
              {canUsePromotion(loadedOrder) && (
                <BookingPromotionSection order={loadedOrder} disabled={hasGiftCards} />
              )}
              {canUseGiftCard(loadedOrder) && (
                <BookingGiftCardSection
                  order={loadedOrder}
                  cover={giftCover}
                  onCoverChange={setGiftCover}
                  onFullyCovered={() => navigate(ROUTES.booking.result(loadedOrder.orderId))}
                />
              )}
              <BookingPaymentMethod value={paymentMethod} onChange={onPaymentMethodChange} />
              <Button disabled={isSubmitting} onClick={onSubmit}>
                {isSubmitting
                  ? "Đang chuyển đến cổng thanh toán..."
                  : `Thanh toán${loadedOrder.payment.netPaidAmount > 0 || hasGiftCards ? " phần còn lại" : ""} ${formatCurrency(giftCover ? giftCover.payableAmount : loadedOrder.payment.outstandingAmount)}`}
              </Button>
            </>
          ) : (
            <EmptyState
              icon={Ticket}
              title="Đơn không còn cần thanh toán"
              description="Đơn đã đủ tiền, đã hủy hoặc đã hết hạn. Xem kết quả để biết trạng thái mới nhất."
              action={<ButtonLink to={ROUTES.booking.result(loadedOrder.orderId)}>Xem kết quả đơn</ButtonLink>}
            />
          )
        }
      </QueryState>

      {showCancel && (
        <Button variant="outline" disabled={isCancelling} onClick={onCancel}>
          {isCancelling
            ? "Đang hủy..."
            : order?.type === OrderType.Exchange
              ? "Hủy đổi suất, giữ vé cũ"
              : "Hủy, chọn lại ghế"}
        </Button>
      )}
    </div>
  );
};
