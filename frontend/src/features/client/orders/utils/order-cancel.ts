import { CancelReason, OrderStatus, PaymentMethod, PaymentStatus, PaymentTransactionType } from "@/types/enum";
import type { OrderDetailDto } from "../types";

export const CUSTOMER_CANCEL_REASONS: CancelReason[] = [
  CancelReason.CustomerChangedMind,
  CancelReason.DuplicateOrder,
  CancelReason.Other,
];

const ONLINE_REFUNDABLE_METHODS = new Set<string>([
  PaymentMethod.VNPay,
  PaymentMethod.Momo,
  PaymentMethod.Stripe,
  "GiftCard",
]);

export type OrderCancelMode = "none" | "unpaid" | "refundable" | "counter";

export const getOrderCancelMode = (order: OrderDetailDto): OrderCancelMode => {
  if (order.status === OrderStatus.Pending) return "unpaid";
  if (order.status !== OrderStatus.Confirmed) return "none";

  const paidMethods = order.transactions
    .filter(
      (transaction) =>
        transaction.type === PaymentTransactionType.Charge && transaction.status === PaymentStatus.Success,
    )
    .map((transaction) => transaction.method);

  return paidMethods.every((method) => ONLINE_REFUNDABLE_METHODS.has(method)) ? "refundable" : "counter";
};

export const getRefundItemStatusLabel = (status: string) => {
  if (status === PaymentStatus.Success) return "Đã hoàn";
  if (status === PaymentStatus.Pending) return "Đang xử lý";
  return "Rạp sẽ hoàn thủ công";
};
