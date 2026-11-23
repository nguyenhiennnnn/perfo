import { IdCard } from "lucide-react";
import { PromotionCodeBox } from "@/components/common/promotion-code-box";
import { useActiveCombos } from "@/features/client/combos/hooks/use-combos";

import type { CounterFlow } from "../hooks/use-counter-flow";
import { CounterPriceSummary } from "./counter-price-summary";
import { CounterCheckoutPanel } from "./counter-checkout-panel";

interface CounterCheckoutStepProps {
  flow: CounterFlow;
}

export function CounterCheckoutStep({ flow }: CounterCheckoutStepProps) {
  const activeCombos = useActiveCombos();

  if (!flow.order) return null;

  const cardSeats = flow.seatTicketTypes.cardRequiredSeats;

  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-4">
      <CounterPriceSummary
        seats={flow.selectedSeatsInfo}
        combos={activeCombos.data}
        comboQuantities={flow.comboQuantities}
        discountAmount={flow.order.discountAmount}
        finalAmount={flow.order.finalAmount}
      />
      {cardSeats.length > 0 && (
        <p
          role="note"
          className="flex items-start gap-2 bg-warning/10 p-3 text-sm text-foreground"
        >
          <IdCard className="mt-0.5 size-4 shrink-0 text-warning" />
          <span>
            Nhắc khách xuất trình thẻ cho ghế{" "}
            {cardSeats.map((s) => `${s.seatRow}${s.seatNumber}`).join(", ")}. Nhân viên soát vé sẽ
            kiểm tra lại khi khách vào rạp.
          </span>
        </p>
      )}
      <PromotionCodeBox
        inputId="counter-promotion-code"
        title="Mã khuyến mãi của khách"
        appliedCode={flow.promotion.appliedCode}
        appliedName={flow.promotion.appliedName}
        discountAmount={flow.promotion.appliedDiscount}
        onApply={flow.promotion.applyPromotion}
        onRemove={flow.promotion.removePromotion}
        disabled={flow.hasPartialPayment || flow.isPaying || flow.isCancelling}
        disabledHint={
          flow.hasPartialPayment
            ? "Đơn đã thu một phần nên không thể đổi mã khuyến mãi."
            : undefined
        }
      />
      <CounterCheckoutPanel
        order={flow.order}
        payments={flow.payments}
        outstandingAmount={flow.outstandingAmount}
        hasPartialPayment={flow.hasPartialPayment}
        onPay={flow.pay}
        onChangeSeats={flow.backToSeats}
        onCancelOrder={flow.cancelOrder}
        isSubmitting={flow.isPaying || flow.isCancelling}
      />
    </div>
  );
}
