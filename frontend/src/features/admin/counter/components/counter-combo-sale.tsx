import { cn } from "@/lib/utils";
import type { CounterComboSaleFlow } from "../hooks/use-counter-combo-sale";
import { CounterComboBuildStep } from "./counter-combo-build-step";
import { CounterComboPaymentPanel } from "./counter-combo-payment-panel";
import { CounterComboReceipt } from "./counter-combo-receipt";

interface CounterComboSaleProps {
  sale: CounterComboSaleFlow;
}

export const CounterComboSale = ({ sale }: CounterComboSaleProps) => {
  if (sale.step === "done" && sale.receipt) {
    return (
      <CounterComboReceipt
        order={sale.order}
        lines={sale.orderLines}
        receipt={sale.receipt}
        onNewSale={() => void sale.reset()}
      />
    );
  }

  return (
    <>
      <div className={cn(sale.step !== "build" && "hidden")}>
        <CounterComboBuildStep sale={sale} />
      </div>
      {sale.step === "payment" && sale.order && (
        <CounterComboPaymentPanel
          order={sale.order}
          lines={sale.orderLines}
          onPay={(method, amountReceived) => void sale.pay(method, amountReceived)}
          onBack={sale.backToBuild}
          onCancel={sale.reset}
          isSubmitting={sale.isPaying || sale.isReleasing}
        />
      )}
    </>
  );
};
