import { TriangleAlert } from "lucide-react";
import { PaymentStatusBadge } from "@/components/common/status-badge";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { PaymentTransactionType } from "@/types/enum";
import type { OrderTransactionDto } from "../types";
import { getPaymentMethodLabel } from "../utils/order-cancel";
import { getTransactionTypeLabel } from "../utils/order-detail";

const Field = ({ label, value }: { label: string; value: string }) => (
  <div className="flex min-w-0 flex-col gap-0.5">
    <dt className="text-xs text-muted-foreground">{label}</dt>
    <dd className="text-sm wrap-break-word text-foreground">{value}</dd>
  </div>
);

interface TransactionItemProps {
  transaction: OrderTransactionDto;
  originalCode?: string;
}

const TransactionItem = ({ transaction, originalCode }: TransactionItemProps) => {
  const isRefund = transaction.type === PaymentTransactionType.Refund;

  return (
    <li className="flex flex-col gap-3 py-3 first:pt-0 last:pb-0">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <Badge variant={isRefund ? "warning" : "outline"}>
            {getTransactionTypeLabel(transaction.type)}
          </Badge>
          <span className="text-sm font-medium text-foreground">
            {getPaymentMethodLabel(transaction.method)}
          </span>
          <PaymentStatusBadge status={transaction.status} />
          {transaction.needsManualRefund && (
            <Badge variant="destructive">
              <TriangleAlert />
              Cần hoàn thủ công
            </Badge>
          )}
        </div>
        <span className="text-sm font-semibold tabular-nums text-foreground">
          {isRefund ? "-" : ""}
          {formatCurrency(transaction.amount)}
        </span>
      </div>

      <dl className="grid grid-cols-1 gap-x-4 gap-y-2 sm:grid-cols-2">
        <Field label="Mã giao dịch" value={transaction.transactionCode} />
        {transaction.gatewayTransactionId && (
          <Field label="Mã cổng thanh toán" value={transaction.gatewayTransactionId} />
        )}
        {transaction.gatewayResponseCode && (
          <Field label="Mã phản hồi cổng" value={transaction.gatewayResponseCode} />
        )}
        {transaction.gatewayFee != null && (
          <Field label="Phí cổng" value={formatCurrency(transaction.gatewayFee)} />
        )}
        {transaction.amountReceived != null && (
          <Field label="Tiền nhận" value={formatCurrency(transaction.amountReceived)} />
        )}
        {transaction.changeGiven != null && (
          <Field label="Tiền thối" value={formatCurrency(transaction.changeGiven)} />
        )}
        {transaction.originalTransactionId != null && (
          <Field
            label="Giao dịch gốc"
            value={originalCode ?? `#${transaction.originalTransactionId}`}
          />
        )}
        <Field label="Thời điểm tạo" value={formatDateTime(transaction.createdAt)} />
        {transaction.completedAt && (
          <Field label="Hoàn tất lúc" value={formatDateTime(transaction.completedAt)} />
        )}
        {transaction.note && <Field label="Ghi chú" value={transaction.note} />}
      </dl>
    </li>
  );
};

interface OrderTransactionsCardProps {
  transactions: OrderTransactionDto[];
}

export const OrderTransactionsCard = ({ transactions }: OrderTransactionsCardProps) => {
  const codeById = new Map(transactions.map((item) => [item.transactionId, item.transactionCode]));

  return (
    <section className="cut-md bg-card p-4">
      <h2 className="t-h3 mb-3 text-foreground">Sổ giao dịch</h2>
      {transactions.length === 0 ? (
        <p className="text-sm text-muted-foreground">Đơn hàng chưa có giao dịch thu hoặc hoàn.</p>
      ) : (
        <ul className="divide-y divide-border">
          {transactions.map((transaction) => (
            <TransactionItem
              key={transaction.transactionId}
              transaction={transaction}
              originalCode={
                transaction.originalTransactionId != null
                  ? codeById.get(transaction.originalTransactionId)
                  : undefined
              }
            />
          ))}
        </ul>
      )}
    </section>
  );
};
