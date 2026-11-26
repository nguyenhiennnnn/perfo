import { Separator } from "@/components/ui/separator";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { OrderLineType } from "@/types/enum";
import type { OrderDetailDto, OrderLineDto } from "../../types";
import { getTicketLines } from "../../utils/order-lines";
import { getAdjustmentLabel } from "../../utils/order-labels";
import { getBundleComponentLabel, getVisibleAdjustments, groupBundleLines } from "../../utils/order-bundles";

const getLineLabel = (line: OrderLineDto) => {
  const name = line.combo?.name ?? line.description ?? "";
  return line.lineType === OrderLineType.Combo || line.quantity > 1 ? `${line.quantity}x ${name}` : name;
};

interface SummaryRowProps {
  label: string;
  value: string;
  valueClassName?: string;
  testId?: string;
}

const SummaryRow = ({ label, value, valueClassName, testId }: SummaryRowProps) => (
  <div className="flex items-start justify-between gap-3 text-sm">
    <span className="text-muted-foreground">{label}</span>
    <span data-testid={testId} className={cn("shrink-0 font-medium tabular-nums", valueClassName)}>
      {value}
    </span>
  </div>
);

interface BookingPaymentTotalsProps {
  order: OrderDetailDto;
}

export const BookingPaymentTotals = ({ order }: BookingPaymentTotalsProps) => (
  <div className="flex flex-col gap-2">
    <SummaryRow label="Tổng phải trả" value={formatCurrency(order.finalAmount)} />
    <SummaryRow
      label="Đã trả"
      value={formatCurrency(order.payment.netPaidAmount)}
      testId="order-paid-amount"
    />
    <div className="flex items-center justify-between gap-3">
      <span className="font-medium text-foreground">Còn phải trả</span>
      <span
        data-testid="order-outstanding-amount"
        className="text-lg font-bold tabular-nums text-primary-text"
      >
        {formatCurrency(order.payment.outstandingAmount)}
      </span>
    </div>
  </div>
);

interface BookingOrderSummaryProps {
  order: OrderDetailDto;
}

export const BookingOrderSummary = ({ order }: BookingOrderSummaryProps) => {
  const grouping = groupBundleLines(order);
  const ticketLines = getTicketLines(order).filter((line) => !grouping.bundledLineIds.has(line.orderLineId));
  const otherLines = order.lines.filter(
    (line) => line.lineType !== OrderLineType.Ticket && !grouping.bundledLineIds.has(line.orderLineId),
  );
  const seatLabels = ticketLines.map((line) => `${line.ticket.seatRow}${line.ticket.seatNumber}`);
  const ticketTotal = ticketLines.reduce((sum, line) => sum + line.lineTotal, 0);

  return (
    <div className="flex flex-col gap-2 bg-card p-4">
      <h3 className="t-h3 mb-1 text-foreground">Chi tiết đơn hàng</h3>
      {grouping.groups.map((group) => (
        <div key={group.adjustmentId} className="flex flex-col gap-1">
          <SummaryRow
            label={group.title}
            value={formatCurrency(group.packagePrice)}
            valueClassName="font-semibold"
          />
          {group.components.map(({ line, splitPrice }) => (
            <div
              key={line.orderLineId}
              className="flex items-start justify-between gap-3 pl-3 text-xs text-muted-foreground"
            >
              <span>{getBundleComponentLabel(line)}</span>
              <span className="shrink-0 tabular-nums">{formatCurrency(splitPrice)}</span>
            </div>
          ))}
        </div>
      ))}
      {ticketLines.length > 0 && (
        <SummaryRow
          label={`Ghế (${ticketLines.length}): ${seatLabels.join(", ")}`}
          value={formatCurrency(ticketTotal)}
        />
      )}
      {otherLines.map((line) => (
        <SummaryRow key={line.orderLineId} label={getLineLabel(line)} value={formatCurrency(line.lineTotal)} />
      ))}
      {getVisibleAdjustments(order, grouping).map((adjustment) => (
        <SummaryRow
          key={adjustment.adjustmentId}
          label={getAdjustmentLabel(adjustment)}
          value={`-${formatCurrency(adjustment.amount)}`}
          valueClassName="text-primary-text"
        />
      ))}
      <Separator />
      <BookingPaymentTotals order={order} />
    </div>
  );
};
