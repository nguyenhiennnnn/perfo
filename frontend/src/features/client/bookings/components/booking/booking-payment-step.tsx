import { Button } from "@/components/ui/button";
import { PaymentMethod } from "@/types/enum";
import type { HoldBookingResponse, PricedSeat } from "@/features/client/bookings/types";
import { BookingHoldTimer } from "./booking-hold-timer";
import { BookingPaymentMethod } from "./booking-payment-method";
import { BookingPriceSummary, type BookingComboLine } from "./booking-price-summary";
import { formatCurrency } from "@/lib/formatters";

interface BookingPaymentStepProps {
  seats: PricedSeat[];
  comboItems: BookingComboLine[];
  hold: HoldBookingResponse;
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