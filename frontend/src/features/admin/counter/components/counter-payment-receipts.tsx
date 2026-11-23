import { formatCurrency } from "@/lib/formatters";
import { getPaymentMethodLabel } from "@/features/admin/orders/utils/order-cancel";
import type { CounterCheckoutResponse } from "../types";

interface CounterPaymentReceiptsProps {
  payments: CounterCheckoutResponse[];
}

export const CounterPaymentReceipts = ({ payments }: CounterPaymentReceiptsProps) => {
  if (payments.length === 0) return null;

  return (
    <ul className="flex flex-col gap-1.5 bg-muted p-3 text-sm" aria-label="Các lần đã thu">
      {payments.map((payment) => (
        <li
          key={payment.paymentTransactionId}
          className="flex items-start justify-between gap-3"
        >
          <span className="min-w-0 text-muted-foreground">
            {getPaymentMethodLabel(payment.paymentMethod)}
            {payment.amountReceived != null && (
              <span className="block text-xs">
                Nhận {formatCurrency(payment.amountReceived)}, thối {formatCurrency(payment.change)}
              </span>
            )}
          </span>
          <span className="font-medium tabular-nums text-foreground">
            {formatCurrency(payment.amountPaid)}
          </span>
        </li>
      ))}
    </ul>
  );
};
