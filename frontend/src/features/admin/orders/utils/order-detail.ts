import { OrderLineType, PaymentTransactionType } from "@/types/enum";
import type { OrderDetailDto } from "../types";

const TRANSACTION_TYPE_LABELS: Record<string, string> = {
  [PaymentTransactionType.Charge]: "Thu tiền",
  [PaymentTransactionType.Refund]: "Hoàn tiền",
};

const LINE_TYPE_LABELS: Record<string, string> = {
  [OrderLineType.Ticket]: "Vé",
  [OrderLineType.Combo]: "Combo",
  [OrderLineType.GiftCard]: "Thẻ quà tặng",
  [OrderLineType.Fee]: "Phí",
};

export const getTransactionTypeLabel = (type: string) =>
  TRANSACTION_TYPE_LABELS[type] ?? type;

export const getLineTypeLabel = (type: string) => LINE_TYPE_LABELS[type] ?? type;

export const hasTicketLines = (order: OrderDetailDto) =>
  order.lines.some((line) => line.lineType === OrderLineType.Ticket);
