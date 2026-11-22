import { formatCurrency } from "@/lib/formatters";
import { getAdjustmentLabel } from "@/features/client/orders/utils/order-labels";
import type { OrderDetailDto } from "../types";
import { InfoRow } from "./order-detail-parts";

const Money = ({ value, negative }: { value: number; negative?: boolean }) => (
  <span className="tabular-nums">
    {negative && value > 0 ? "-" : ""}
    {formatCurrency(value)}
  </span>
);

interface OrderSummaryCardProps {
  order: OrderDetailDto;
}

export const OrderSummaryCard = ({ order }: OrderSummaryCardProps) => {
  const { payment } = order;

  return (
    <section className="cut-md bg-card p-4">
      <h2 className="t-h3 mb-3 text-foreground">Giá và thanh toán</h2>
      <dl className="flex flex-col gap-2">
        <InfoRow label="Tổng gốc">
          <Money value={order.totalAmount} />
        </InfoRow>
        {order.priceAdjustments.map((adjustment) => (
          <InfoRow key={adjustment.adjustmentId} label={getAdjustmentLabel(adjustment)}>
            <Money value={adjustment.amount} negative />
          </InfoRow>
        ))}
        <InfoRow label="Giảm giá">
          <Money value={order.discountAmount} negative />
        </InfoRow>
        <div className="flex items-start justify-between gap-3 border-t border-border pt-2 text-sm">
          <dt className="font-medium text-foreground">Phải trả</dt>
          <dd className="font-semibold tabular-nums text-primary-text">
            {formatCurrency(order.finalAmount)}
          </dd>
        </div>
      </dl>

      <dl className="mt-4 flex flex-col gap-2 border-t border-border pt-4">
        <InfoRow label="Đã trả">
          <Money value={payment.paidAmount} />
        </InfoRow>
        <InfoRow label="Đã hoàn">
          <Money value={payment.refundedAmount} />
        </InfoRow>
        <InfoRow label="Chờ hoàn">
          <Money value={payment.pendingRefundAmount} />
        </InfoRow>
        <InfoRow label="Thực thu">
          <Money value={payment.netPaidAmount} />
        </InfoRow>
        <InfoRow label="Còn thiếu">
          <Money value={payment.outstandingAmount} />
        </InfoRow>
        <InfoRow label="Thừa">
          <Money value={payment.overpaidAmount} />
        </InfoRow>
      </dl>
    </section>
  );
};
