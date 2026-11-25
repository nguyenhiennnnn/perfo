import { OrderLineType } from "@/types/enum";
import type {
  OrderDetailDto,
  OrderLineDto,
  OrderTicketDto,
} from "../types";

export type OrderTicketLine = OrderLineDto & { ticket: OrderTicketDto };

export const getTicketLines = (order: OrderDetailDto): OrderTicketLine[] =>
  order.lines.filter(
    (line): line is OrderTicketLine =>
      line.lineType === OrderLineType.Ticket && line.ticket != null,
  );

export const isTicketRefunded = (line: OrderLineDto) =>
  line.refundedQuantity >= line.quantity;

export const getActiveTicketLines = (order: OrderDetailDto): OrderTicketLine[] => {
  const exchangedOut = new Set(
    (order.exchanges ?? [])
      .filter((exchange) => exchange.originalOrderId === order.orderId)
      .map((exchange) => exchange.oldOrderLineId),
  );
  return getTicketLines(order).filter(
    (line) => !isTicketRefunded(line) && !exchangedOut.has(line.orderLineId),
  );
};

export const getOrderShowing = (order: OrderDetailDto) =>
  getTicketLines(order)[0]?.ticket ?? null;

export const getOrderSeats = (order: OrderDetailDto): OrderTicketDto[] =>
  getTicketLines(order).map((line) => line.ticket);
