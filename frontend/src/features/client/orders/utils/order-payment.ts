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

export type TransactionTone = "success" | "warning" | "destructive" | "secondary";

export interface TransactionView {
  isRefund: boolean;
  statusLabel: string;
  tone: TransactionTone;
}

export const getTransactionView = (transaction: OrderTransactionDto): TransactionView => {
  const isRefund = transaction.type === PaymentTransactionType.Refund;

  if (transaction.status === PaymentStatus.Failed) {
    return { isRefund, statusLabel: isRefund ? "Hoàn tiền thất bại" : "Thất bại", tone: "destructive" };
  }
  if (transaction.status === PaymentStatus.Pending) {
    if (isRefund && transaction.needsManualRefund) {
      return { isRefund, statusLabel: "Chờ rạp hoàn tiền mặt", tone: "warning" };
    }
    return { isRefund, statusLabel: isRefund ? "Đang hoàn tiền" : "Đang xử lý", tone: "warning" };
  }
  return { isRefund, statusLabel: isRefund ? "Đã hoàn" : "Thành công", tone: "success" };
};
