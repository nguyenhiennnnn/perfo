import { Button } from "@/components/ui/button";
import { PaymentMethod } from "@/types/enum";
import type { HoldOrderResponse, OrderComboLine, PricedSeat } from "@/features/client/orders/types";
import { BookingHoldTimer } from "./booking-hold-timer";
import { BookingPaymentMethod } from "./booking-payment-method";
import { BookingPriceSummary } from "./booking-price-summary";
import { formatCurrency } from "@/lib/formatters";

interface BookingPaymentStepProps {
  seats: PricedSeat[];
  comboItems: OrderComboLine[];
  hold: HoldOrderResponse;
  paymentMethod: PaymentMethod;
  onPaymentMethodChange: (value: PaymentMethod) => void;
  isSubmitting: boolean;
  onSubmit: () => void;
  onCancel: () => void;
  isCancelling: boolean;
}

export const BookingPaymentStep = ({
  seats,
  comboItems,
  hold,
  paymentMethod,
  onPaymentMethodChange,
  isSubmitting,
  onSubmit,
  onCancel,
  isCancelling,
}: BookingPaymentStepProps) => {
  return (
    <div className="mx-auto flex w-full max-w-lg flex-col gap-4">
      <BookingHoldTimer
        expiresAt={hold.expiresAt}
        className="sticky top-16 z-40"
      />

      <BookingPriceSummary seats={seats} combos={undefined} comboQuantities={{}} comboItems={comboItems} hold={hold} />

      <BookingPaymentMethod value={paymentMethod} onChange={onPaymentMethodChange} />

      <Button disabled={isSubmitting} onClick={onSubmit}>
        {isSubmitting ? "Đang chuyển đến cổng thanh toán..." : `Thanh toán ${formatCurrency(hold.finalAmount)}`}
      </Button>
      <Button variant="outline" disabled={isCancelling} onClick={onCancel}>
        {isCancelling ? "Đang hủy..." : "Hủy, chọn lại ghế"}
      </Button>
    </div>
  );
}