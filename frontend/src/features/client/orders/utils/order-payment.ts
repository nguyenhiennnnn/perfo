import { OrderStatus, PaymentStatus, PaymentTransactionType } from "@/types/enum";
import type { OrderDetailDto, OrderTransactionDto } from "../types";

export type PaymentResultState =
  | "confirmed"
  | "processing"
  | "outstanding"
  | "failed"
  | "expired"
  | "cancelled";

const byNewest = (a: OrderTransactionDto, b: OrderTransactionDto) =>
  new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();

export const getChargeTransactions = (order: OrderDetailDto) =>
  order.transactions
    .filter((transaction) => transaction.type === PaymentTransactionType.Charge)
    .sort(byNewest);

export const getPaymentResultState = (
  order: OrderDetailDto,
  gatewayCanceled = false,
): PaymentResultState => {
  if (order.status === OrderStatus.Confirmed) return "confirmed";
  if (order.status === OrderStatus.Expired) return "expired";
  if (order.status === OrderStatus.Cancelled) return "cancelled";

  if (order.payment.outstandingAmount <= 0) return "processing";
  if (gatewayCanceled) return "failed";

  const charges = getChargeTransactions(order);
  if (charges.some((charge) => charge.status === PaymentStatus.Pending)) return "processing";
  if (charges[0]?.status === PaymentStatus.Failed) return "failed";
  return "outstanding";
};

export const canPayOutstanding = (order: OrderDetailDto) =>
  order.status === OrderStatus.Pending && order.payment.outstandingAmount > 0;
