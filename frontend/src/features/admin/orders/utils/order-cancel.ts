import {
  CancelReason,
  OrderLineType,
  OrderStatus,
  PaymentStatus,
  PaymentTransactionType,
} from "@/types/enum";
import type { CancelOrderRefundDto } from "../types";

const CASH_METHOD = "Cash";

export const ADMIN_CANCEL_REASONS: CancelReason[] = [
  CancelReason.CustomerChangedMind,
  CancelReason.DuplicateOrder,
  CancelReason.PaymentError,
  CancelReason.Other,
];

const CANCEL_REASON_LABELS: Record<string, string> = {
  [CancelReason.CustomerChangedMind]: "Khách đổi ý",
  [CancelReason.DuplicateOrder]: "Đơn trùng",
  [CancelReason.PaymentError]: "Lỗi thanh toán",
  [CancelReason.ShowtimeCancelled]: "Rạp hủy suất chiếu",
  [CancelReason.RoomClosed]: "Rạp đóng phòng chiếu",
  [CancelReason.Other]: "Lý do khác",
};

const CANCELLED_BY_LABELS: Record<string, string> = {
  Customer: "Khách hàng",
  Staff: "Nhân viên",
  Admin: "Quản trị viên",
  System: "Hệ thống",
};

const PAYMENT_METHOD_LABELS: Record<string, string> = {
  Cash: "Tiền mặt",
  Card: "Thẻ",
  VNPay: "VNPay",
  Momo: "MoMo",
  ZaloPay: "ZaloPay",
  Stripe: "Stripe",
  GiftCard: "Thẻ quà tặng",
  BankTransfer: "Chuyển khoản",
};

export const getCancelReasonLabel = (reason: string) =>
  CANCEL_REASON_LABELS[reason] ?? reason;

export const getCancelledByLabel = (actor: string) =>
  CANCELLED_BY_LABELS[actor] ?? actor;

export const getPaymentMethodLabel = (method: string) =>
  PAYMENT_METHOD_LABELS[method] ?? method;

export const canCancelOrder = (status: string) =>
  status === OrderStatus.Pending || status === OrderStatus.Confirmed;

export interface RefundStatusInfo {
  label: string;
  variant: "success" | "warning" | "destructive";
}

export const getRefundStatusInfo = (status: string): RefundStatusInfo => {
  if (status === PaymentStatus.Success)
    return { label: "Đã hoàn", variant: "success" };
  if (status === PaymentStatus.Pending)
    return { label: "Chờ hoàn", variant: "warning" };
  return { label: "Cần hoàn thủ công", variant: "destructive" };
};

export const getRefundTotal = (refunds: CancelOrderRefundDto[]) =>
  refunds.reduce((sum, refund) => sum + refund.amount, 0);

export const getCashRefundDue = (refunds: CancelOrderRefundDto[]) =>
  refunds
    .filter(
      (refund) =>
        refund.method === CASH_METHOD && refund.status === PaymentStatus.Pending,
    )
    .reduce((sum, refund) => sum + refund.amount, 0);

export const hasFailedRefund = (refunds: CancelOrderRefundDto[]) =>
  refunds.some(
    (refund) =>
      refund.status !== PaymentStatus.Success &&
      refund.status !== PaymentStatus.Pending,
  );

export interface PaidMethodAmount {
  method: string;
  amount: number;
}

export const getPaidMethodAmounts = (order: {
  transactions: { type: string; status: string; method: string; amount: number }[];
}): PaidMethodAmount[] => {
  const totals = new Map<string, number>();
  for (const transaction of order.transactions) {
    if (
      transaction.type !== PaymentTransactionType.Charge ||
      transaction.status !== PaymentStatus.Success
    )
      continue;
    totals.set(
      transaction.method,
      (totals.get(transaction.method) ?? 0) + transaction.amount,
    );
  }
  return Array.from(totals, ([method, amount]) => ({ method, amount }));
};

export interface CheckInSummary {
  total: number;
  checkedIn: number;
}

export const getCheckInSummary = (order: {
  lines: { lineType: string; ticket?: { isCheckedIn: boolean } | null }[];
}): CheckInSummary => {
  const tickets = order.lines.filter(
    (line) => line.lineType === OrderLineType.Ticket && line.ticket,
  );
  return {
    total: tickets.length,
    checkedIn: tickets.filter((line) => line.ticket?.isCheckedIn).length,
  };
};
