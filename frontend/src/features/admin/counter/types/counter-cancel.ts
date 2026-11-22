import type { OrderStatus } from "@/types/enum";

export interface CounterOrderSearchParams {
  search: string;
  page: number;
  pageSize: number;
}

export interface CounterOrderListItem {
  orderId: number;
  orderCode: string;
  type: string;
  channel: string;
  status: OrderStatus;
  customerName?: string | null;
  customerPhone?: string | null;
  movieTitle?: string | null;
  startTime?: string | null;
  ticketCount: number;
  finalAmount: number;
  createdAt: string;
  expiresAt?: string | null;
}

export interface CounterOrderTicket {
  movieTitle: string;
  roomName: string;
  startTime: string;
  seatRow: string;
  seatNumber: number;
  isCheckedIn: boolean;
  checkedInAt?: string | null;
}

export interface CounterOrderCombo {
  name?: string | null;
  quantityFulfilled: number;
}

export interface CounterOrderLine {
  orderLineId: number;
  lineType: string;
  description?: string | null;
  quantity: number;
  lineTotal: number;
  ticket?: CounterOrderTicket | null;
  combo?: CounterOrderCombo | null;
}

export interface CounterOrderPayment {
  paidAmount: number;
  refundedAmount: number;
  pendingRefundAmount: number;
  netPaidAmount: number;
}

export interface CounterOrderTransaction {
  transactionId: number;
  type: string;
  method: string;
  status: string;
  amount: number;
}

export interface CounterOrderDetail {
  orderId: number;
  orderCode: string;
  type: string;
  channel: string;
  status: OrderStatus;
  customerName?: string | null;
  customerPhone?: string | null;
  createdAt: string;
  expiresAt?: string | null;
  finalAmount: number;
  payment: CounterOrderPayment;
  cancelReason?: string | null;
  cancelledBy?: string | null;
  cancelledAt?: string | null;
  lines: CounterOrderLine[];
  transactions: CounterOrderTransaction[];
}
