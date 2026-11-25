import { OrderLineType, OrderStatus, PaymentStatus, PaymentMethod, PaymentTransactionType } from "@/types/enum";
import { getServerNow } from "@/lib/server-clock";
import type { ShowtimeSeatItemDto } from "@/features/client/showtimes/types";
import { getSeatPrice } from "@/features/client/showtimes/utils/seat-pricing";
import type {
  OrderDetailDto,
  OrderExchangeHistoryDto,
  OrderExchangeSettlementKind,
} from "../types";
import { getTicketLines, type OrderTicketLine } from "./order-lines";

export const getExchangedOutLineIds = (order: OrderDetailDto) =>
  new Set(
    (order.exchanges ?? [])
      .filter((exchange) => exchange.originalOrderId === order.orderId)
      .map((exchange) => exchange.oldOrderLineId),
  );

export const getExchangeableLines = (order: OrderDetailDto): OrderTicketLine[] => {
  if (order.status !== OrderStatus.Confirmed) return [];
  const exchangedOut = getExchangedOutLineIds(order);
  const now = getServerNow();
  return getTicketLines(order).filter(
    (line) =>
      line.lineType === OrderLineType.Ticket &&
      !line.ticket.isCheckedIn &&
      line.refundedQuantity === 0 &&
      line.refundedAmount === 0 &&
      !exchangedOut.has(line.orderLineId) &&
      new Date(line.ticket.startTime).getTime() > now,
  );
};

export const hasCashPayment = (order: OrderDetailDto) =>
  order.transactions.some(
    (transaction) =>
      transaction.type === PaymentTransactionType.Charge &&
      transaction.status === PaymentStatus.Success &&
      transaction.method === PaymentMethod.Cash,
  );

export const seatCode = (row: string, number: number) => `${row}${number}`;

export const compareSeats = (
  a: { seatRow: string; seatNumber: number },
  b: { seatRow: string; seatNumber: number },
) => a.seatRow.localeCompare(b.seatRow, "vi", { numeric: true }) || a.seatNumber - b.seatNumber;

export interface ExchangeDifference {
  amountDue: number;
  creditAmount: number;
}

export const toExchangeDifference = (difference: number): ExchangeDifference => ({
  amountDue: Math.max(difference, 0),
  creditAmount: Math.max(-difference, 0),
});

export interface ExchangePairing {
  line: OrderTicketLine;
  seat: ShowtimeSeatItemDto;
  newPrice: number;
}

export const pairExchangeSeats = (
  lines: OrderTicketLine[],
  seats: ShowtimeSeatItemDto[],
): ExchangePairing[] => {
  const sortedLines = [...lines].sort((a, b) => compareSeats(a.ticket, b.ticket));
  const sortedSeats = [...seats].sort(compareSeats);
  return sortedLines.flatMap((line, index) => {
    const seat = sortedSeats[index];
    return seat
      ? [{ line, seat, newPrice: getSeatPrice(seat, line.ticket.ticketTypeId) * line.quantity }]
      : [];
  });
};

export interface ExchangeGroup {
  key: string;
  exchangeNumber: number;
  exchangedAt: string;
  originalOrderId: number;
  originalOrderCode: string;
  newOrderId: number;
  newOrderCode: string;
  channel: string;
  movieTitle: string;
  oldShowtimeId: number;
  oldRoomName: string;
  oldStartTime: string;
  newShowtimeId: number;
  newRoomName: string;
  newStartTime: string;
  priceDifference: number;
  seats: { oldSeat: string; newSeat: string }[];
}

export const groupExchangeHistory = (exchanges: OrderExchangeHistoryDto[]): ExchangeGroup[] => {
  const groups = new Map<string, ExchangeGroup>();
  for (const exchange of exchanges) {
    const key = `${exchange.originalOrderId}-${exchange.exchangeNumber}-${exchange.newOrderId}`;
    const seat = {
      oldSeat: seatCode(exchange.oldSeatRow, exchange.oldSeatNumber),
      newSeat: seatCode(exchange.newSeatRow, exchange.newSeatNumber),
    };
    const existing = groups.get(key);
    if (existing) {
      existing.seats.push(seat);
      existing.priceDifference += exchange.priceDifference;
      continue;
    }
    groups.set(key, {
      key,
      exchangeNumber: exchange.exchangeNumber,
      exchangedAt: exchange.exchangedAt,
      originalOrderId: exchange.originalOrderId,
      originalOrderCode: exchange.originalOrderCode,
      newOrderId: exchange.newOrderId,
      newOrderCode: exchange.newOrderCode,
      channel: exchange.channel,
      movieTitle: exchange.movieTitle,
      oldShowtimeId: exchange.oldShowtimeId,
      oldRoomName: exchange.oldRoomName,
      oldStartTime: exchange.oldStartTime,
      newShowtimeId: exchange.newShowtimeId,
      newRoomName: exchange.newRoomName,
      newStartTime: exchange.newStartTime,
      priceDifference: exchange.priceDifference,
      seats: [seat],
    });
  }
  return [...groups.values()].sort(
    (a, b) => new Date(b.exchangedAt).getTime() - new Date(a.exchangedAt).getTime(),
  );
};

export const isCreditKind = (kind?: OrderExchangeSettlementKind | null) => kind === "ExchangeCredit";
export const isCashRefundKind = (kind?: OrderExchangeSettlementKind | null) => kind === "CashRefund";
