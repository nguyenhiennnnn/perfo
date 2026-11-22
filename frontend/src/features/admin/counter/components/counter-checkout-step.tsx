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

  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-4">
      <CounterPriceSummary
        seats={flow.selectedSeatsInfo}
        combos={activeCombos.data}
        comboQuantities={flow.comboQuantities}
        discountAmount={flow.order.discountAmount}
        finalAmount={flow.order.finalAmount}
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
