import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { OrderLineType } from "@/types/enum";
import type { OrderDetailDto, OrderLineDto } from "../../types";
import { getTicketLines, type OrderTicketLine } from "../../utils/order-lines";

interface LinePriceProps {
  line: OrderLineDto;
}

const LinePrice = ({ line }: LinePriceProps) => (
  <div className="shrink-0 text-right tabular-nums">
    <p className="text-sm font-medium text-foreground">{formatCurrency(line.lineTotal)}</p>
    {line.unitListPrice > line.lockedUnitPrice && (
      <p className="text-xs text-muted-foreground line-through">{formatCurrency(line.unitListPrice * line.quantity)}</p>
    )}
  </div>
);

const RefundNote = ({ line }: LinePriceProps) => {
  if (line.refundedQuantity <= 0 && line.refundedAmount <= 0) return null;
  return (
    <p className="mt-0.5 text-xs text-warning-text">
      Đã hoàn {formatCurrency(line.refundedAmount)}
      {line.quantity > 1 ? ` (${line.refundedQuantity}/${line.quantity})` : ""}
    </p>
  );
};

const TicketLineRow = ({ line }: { line: OrderTicketLine }) => {
  const { ticket } = line;
  const ticketType = line.description?.trim();

  return (
    <li className="flex items-start justify-between gap-3 py-3">
      <div className="min-w-0">
        <p className="text-sm font-medium text-foreground">
          Vé ghế {ticket.seatRow}
          {ticket.seatNumber}
          {ticketType ? `, ${ticketType}` : ""}
        </p>
        <p className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">{ticket.movieTitle}</p>
        <p className="mt-0.5 text-xs text-muted-foreground">
          {formatDateTime(ticket.startTime)}, {ticket.roomName}
        </p>
        <RefundNote line={line} />
      </div>
      <LinePrice line={line} />
    </li>
  );
};

const ComboLineRow = ({ line }: { line: OrderLineDto }) => {
  const name = line.combo?.name ?? line.description ?? "";
  const fulfilled = line.combo?.quantityFulfilled ?? 0;
  const isAllFulfilled = fulfilled >= line.quantity;

  return (
    <li className="flex items-start justify-between gap-3 py-3">
      <div className="min-w-0">
        <p className="text-sm font-medium text-foreground">{name}</p>
        <p className="mt-0.5 text-xs text-muted-foreground">
          Số lượng {line.quantity}, đơn giá {formatCurrency(line.lockedUnitPrice)}
        </p>
        <p className={isAllFulfilled ? "mt-0.5 text-xs text-success" : "mt-0.5 text-xs text-muted-foreground"}>
          {isAllFulfilled ? "Đã nhận đủ" : `Đã nhận ${fulfilled}/${line.quantity}`}
        </p>
        <RefundNote line={line} />
      </div>
      <LinePrice line={line} />
    </li>
  );
};

const OtherLineRow = ({ line }: { line: OrderLineDto }) => (
  <li className="flex items-start justify-between gap-3 py-3">
    <div className="min-w-0">
      <p className="text-sm font-medium text-foreground">{line.description ?? "Khoản khác"}</p>
      {line.quantity > 1 && <p className="mt-0.5 text-xs text-muted-foreground">Số lượng {line.quantity}</p>}
      <RefundNote line={line} />
    </div>
    <LinePrice line={line} />
  </li>
);

interface OrderLinesSectionProps {
  order: OrderDetailDto;
}

export const OrderLinesSection = ({ order }: OrderLinesSectionProps) => {
  const ticketLines = getTicketLines(order);
  const comboLines = order.lines.filter((line) => line.lineType === OrderLineType.Combo);
  const otherLines = order.lines.filter(
    (line) => line.lineType !== OrderLineType.Combo && !(line.lineType === OrderLineType.Ticket && line.ticket != null),
  );

  if (order.lines.length === 0) return null;

  return (
    <section aria-labelledby="order-lines-title" className="cut-md bg-card p-4">
      <h2 id="order-lines-title" className="t-h2 text-foreground">
        Dòng hàng
      </h2>
      <ul className="mt-2 divide-y divide-border">
        {ticketLines.map((line) => (
          <TicketLineRow key={line.orderLineId} line={line} />
        ))}
        {comboLines.map((line) => (
          <ComboLineRow key={line.orderLineId} line={line} />
        ))}
        {otherLines.map((line) => (
          <OtherLineRow key={line.orderLineId} line={line} />
        ))}
      </ul>
    </section>
  );
};
