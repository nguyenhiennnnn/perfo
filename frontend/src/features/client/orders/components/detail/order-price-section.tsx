import { Separator } from "@/components/ui/separator";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { OrderDetailDto } from "../../types";
import { getAdjustmentLabel } from "../../utils/order-labels";

interface PriceRowProps {
  label: string;
  value: string;
  valueClassName?: string;
  testId?: string;
}

const PriceRow = ({ label, value, valueClassName, testId }: PriceRowProps) => (
  <div className="flex items-start justify-between gap-3 text-sm">
    <span className="text-muted-foreground">{label}</span>
    <span data-testid={testId} className={cn("shrink-0 font-medium tabular-nums text-foreground", valueClassName)}>
      {value}
    </span>
  </div>
);

interface OrderPriceSectionProps {
  order: OrderDetailDto;
}

export const OrderPriceSection = ({ order }: OrderPriceSectionProps) => {
  const { payment } = order;

  return (
    <section aria-labelledby="order-price-title" className="cut-md bg-card p-4">
      <h2 id="order-price-title" className="t-h2 text-foreground">
        Giá và thanh toán
      </h2>

      <div className="mt-3 flex flex-col gap-2">
        <PriceRow label="Tổng tiền hàng" value={formatCurrency(order.totalAmount)} />
        {order.priceAdjustments.map((adjustment) => (
          <PriceRow
            key={adjustment.adjustmentId}
            label={getAdjustmentLabel(adjustment)}
            value={`-${formatCurrency(adjustment.amount)}`}
            valueClassName="text-primary-text"
          />
        ))}
        <div className="flex items-center justify-between gap-3">
          <span className="font-medium text-foreground">Thành tiền</span>
          <span className="text-base font-semibold tabular-nums text-primary-text">
            {formatCurrency(order.finalAmount)}
          </span>
        </div>
      </div>

      <Separator className="my-3" />

      <div className="flex flex-col gap-2">
        <PriceRow label="Đã trả" value={formatCurrency(payment.paidAmount)} testId="order-paid-amount" />
        <PriceRow label="Đã hoàn" value={formatCurrency(payment.refundedAmount)} testId="order-refunded-amount" />
        {payment.pendingRefundAmount > 0 && (
          <PriceRow
            label="Đang chờ hoàn"
            value={formatCurrency(payment.pendingRefundAmount)}
            valueClassName="text-warning-text"
          />
        )}
        {payment.outstandingAmount > 0 && (
          <div className="flex items-center justify-between gap-3">
            <span className="font-medium text-foreground">Còn thiếu</span>
            <span
              data-testid="order-outstanding-amount"
              className="text-lg font-bold tabular-nums text-primary-text"
            >
              {formatCurrency(payment.outstandingAmount)}
            </span>
          </div>
        )}
        {payment.overpaidAmount > 0 && (
          <PriceRow label="Trả thừa" value={formatCurrency(payment.overpaidAmount)} />
        )}
      </div>
    </section>
  );
};
