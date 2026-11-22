import { ArrowDownLeft, ArrowUpRight } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import type { OrderDetailDto, OrderTransactionDto } from "../../types";
import { getPaymentMethodLabel } from "../../utils/order-labels";
import { getTransactionView } from "../../utils/order-payment";

const TransactionRow = ({ transaction }: { transaction: OrderTransactionDto }) => {
  const view = getTransactionView(transaction);
  const Icon = view.isRefund ? ArrowDownLeft : ArrowUpRight;
  const isCashCharge = !view.isRefund && transaction.amountReceived != null;

  return (
    <li className="flex flex-col gap-2 py-3 sm:flex-row sm:items-start sm:justify-between">
      <div className="flex min-w-0 items-start gap-3">
        <div className="flex size-9 shrink-0 items-center justify-center cut-xs bg-secondary text-muted-foreground">
          <Icon className="size-4" />
        </div>
        <div className="min-w-0">
          <p className="text-sm font-medium text-foreground">
            {view.isRefund ? "Hoàn tiền" : "Thanh toán"} qua {getPaymentMethodLabel(transaction.method)}
          </p>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {formatDateTime(transaction.completedAt ?? transaction.createdAt)}
          </p>
          <p className="mt-0.5 t-code text-xs break-all text-muted-foreground">{transaction.transactionCode}</p>
          {isCashCharge && (
            <p className="mt-0.5 text-xs text-muted-foreground">
              Nhận {formatCurrency(transaction.amountReceived)}, thối {formatCurrency(transaction.changeGiven ?? 0)}
            </p>
          )}
          {transaction.needsManualRefund && (
            <p className="mt-0.5 text-xs text-warning-text">Nhận tiền hoàn trực tiếp tại quầy của rạp.</p>
          )}
        </div>
      </div>
      <div className="flex items-center justify-between gap-3 pl-12 sm:flex-col sm:items-end sm:justify-start sm:gap-1 sm:pl-0">
        <span className="text-sm font-semibold tabular-nums text-foreground">
          {formatCurrency(transaction.amount)}
        </span>
        <Badge variant={view.tone}>{view.statusLabel}</Badge>
      </div>
    </li>
  );
};

interface OrderTransactionsSectionProps {
  order: OrderDetailDto;
}

export const OrderTransactionsSection = ({ order }: OrderTransactionsSectionProps) => (
  <section aria-labelledby="order-transactions-title" className="cut-md bg-card p-4">
    <h2 id="order-transactions-title" className="t-h2 text-foreground">
      Giao dịch thu và hoàn
    </h2>
    {order.transactions.length === 0 ? (
      <p className="mt-3 text-sm text-muted-foreground">Đơn chưa có giao dịch nào.</p>
    ) : (
      <ul className="mt-2 divide-y divide-border">
        {order.transactions.map((transaction) => (
          <TransactionRow key={transaction.transactionId} transaction={transaction} />
        ))}
      </ul>
    )}
  </section>
);
