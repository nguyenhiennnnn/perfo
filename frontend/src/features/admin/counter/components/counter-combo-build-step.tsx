import { ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ComboPicker } from "@/components/common/combo-picker/combo-picker";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import type { CounterComboSaleFlow } from "../hooks/use-counter-combo-sale";
import { CounterCustomerForm } from "./counter-customer-form";
import { CounterComboSummary } from "./counter-combo-summary";

interface CounterComboBuildStepProps {
  sale: CounterComboSaleFlow;
}

export const CounterComboBuildStep = ({ sale }: CounterComboBuildStepProps) => (
  <div className="grid grid-cols-1 gap-6 md:grid-cols-5">
    <div className="bg-card p-4 md:col-span-3">
      <h3 className="t-h3 mb-3 text-foreground">Chọn combo</h3>
      <ComboPicker
        combos={sale.combos.data}
        isLoading={sale.combos.isLoading}
        isError={sale.combos.isError}
        error={sale.combos.error}
        onRetry={() => void sale.combos.refetch()}
        quantities={sale.quantities}
        onChange={sale.setComboQuantity}
      />
    </div>

    <div className="flex flex-col gap-4 md:col-span-2">
      <div className="bg-card p-4">
        <h3 className="t-h3 mb-3 text-foreground">Thông tin khách</h3>
        <CounterCustomerForm
          customerName={sale.customerName}
          onCustomerNameChange={sale.setCustomerName}
          customerPhone={sale.customerPhone}
          onCustomerPhoneChange={sale.setCustomerPhone}
          userId={sale.userId}
          onUserIdChange={sale.setUserId}
        />
      </div>

      <CounterComboSummary lines={sale.draftLines} total={sale.draftTotal} />

      <Button
        disabled={!sale.canCreateOrder || sale.isCreatingOrder}
        onClick={() => void sale.submitOrder()}
      >
        <span>
          {sale.isCreatingOrder ? FORM_PROCESSING_LABEL : "Tiếp tục thanh toán"}
        </span>
        <ArrowRight className="size-4" />
      </Button>
    </div>
  </div>
);
