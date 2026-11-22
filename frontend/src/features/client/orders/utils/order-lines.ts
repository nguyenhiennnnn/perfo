import { OrderLineType } from "@/types/enum";
import type {
  OrderDetailDto,
  OrderLineDto,
  OrderTicketDto,
  PricedSeat,
} from "../types";

export type OrderTicketLine = OrderLineDto & { ticket: OrderTicketDto };

export const getTicketLines = (order: OrderDetailDto): OrderTicketLine[] =>
  order.lines.filter(
    (line): line is OrderTicketLine =>
      line.lineType === OrderLineType.Ticket && line.ticket != null,
  );

export const getOrderSeats = (order: OrderDetailDto): PricedSeat[] =>
  getTicketLines(order).map((line) => ({
    seatRow: line.ticket.seatRow,
    seatNumber: line.ticket.seatNumber,
    price: line.lockedUnitPrice,
  }));

export const getOrderShowing = (order: OrderDetailDto) =>
  getTicketLines(order)[0]?.ticket ?? null;
