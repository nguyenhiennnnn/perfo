import { CheckCircle2, PlusCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/lib/formatters";
import type { CounterComboLine } from "../hooks/use-counter-combo-sale";
import {
  CounterPaymentMethod,
  type CounterCheckoutResponse,
  type CounterOrderResponse,
} from "../types";
import { CounterComboSummary } from "./counter-combo-summary";

interface CounterComboReceiptProps {
  order: CounterOrderResponse | null;
  lines: CounterComboLine[];
  receipt: CounterCheckoutResponse;
  onNewSale: () => void;
}

const METHOD_LABEL: Record<string, string> = {
  [CounterPaymentMethod.Cash]: "Tiền mặt",
  [CounterPaymentMethod.Card]: "Thẻ",
};

export const CounterComboReceipt = ({
  order,
  lines,
  receipt,
  onNewSale,
}: CounterComboReceiptProps) => {
  const isCash = receipt.paymentMethod === CounterPaymentMethod.Cash;

  return (
    <div className="mx-auto flex w-full max-w-md flex-col items-center gap-6 py-6 text-center">
      <CheckCircle2 className="size-10 text-success" />
      <div>
        <p className="font-heading text-lg font-semibold text-foreground">
          Thanh toán thành công
        </p>
        <p className="t-code text-sm text-muted-foreground">{receipt.orderCode}</p>
      </div>

      <div className="flex w-full flex-col gap-3 text-left">
        <CounterComboSummary lines={lines} total={receipt.amountPaid} />
        <div className="flex flex-col gap-2 bg-card p-4 text-sm">
          {order?.customerName && (
            <div className="flex items-center justify-between gap-3">
              <span className="text-muted-foreground">Khách hàng</span>
              <span className="min-w-0 wrap-break-word font-medium text-foreground">
                {order.customerName}
              </span>
            </div>
          )}
          <div className="flex items-center justify-between gap-3">
            <span className="text-muted-foreground">Phương thức</span>
            <span className="font-medium text-foreground">
              {METHOD_LABEL[receipt.paymentMethod] ?? receipt.paymentMethod}
            </span>
          </div>
          {isCash && (
            <>
              <div className="flex items-center justify-between gap-3">
                <span className="text-muted-foreground">Khách đưa</span>
                <span className="font-medium tabular-nums text-foreground">
                  {formatCurrency(receipt.amountReceived ?? receipt.amountPaid)}
                </span>
              </div>
              <div className="flex items-center justify-between gap-3">
                <span className="text-muted-foreground">Tiền thừa trả khách</span>
                <span className="font-semibold tabular-nums text-foreground">
                  {formatCurrency(receipt.change)}
                </span>
              </div>
            </>
          )}
        </div>
      </div>

      <Button onClick={onNewSale}>
        <PlusCircle className="size-4" />
        <span>Bán đơn mới</span>
      </Button>
    </div>
  );
};
