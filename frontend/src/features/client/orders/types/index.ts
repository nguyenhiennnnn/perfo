import type { CancelReason, OrderStatus } from "@/types/enum";

export interface ComboSelectionRequest {
  comboId: number;
  quantity: number;
}

export interface HoldSeatsRequest {
  showtimeId: number;
  customerName: string;
  customerPhone?: string | null;
  seatIds: number[];
  combos?: ComboSelectionRequest[] | null;
}

export interface HoldOrderResponse {
  orderId: number;
  orderCode: string;
  status: OrderStatus;
  totalAmount: number;
  discountAmount: number;
  finalAmount: number;
  expiresAt: string;
}

export interface CheckoutRequest {
  paymentMethod: string;
}

export interface CheckoutResponse {
  paymentUrl: string;
  paymentId: number;
}

export interface CancelOrderRequest {
  reason: CancelReason;
}

export interface CancelOrderRefundDto {
  method: string;
  amount: number;
  status: string;
}

export interface CancelOrderResponse {
  orderId: number;
  orderCode: string;
  status: OrderStatus;
  totalRefunded: number;
  refundStatus: "None" | "Refunded" | "Pending";
  refunds: CancelOrderRefundDto[];
}

export interface OrderListParams {
  status?: string;
  cursor?: string;
  limit?: number;
}

export interface OrderListItemDto {
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

export interface OrderEmployeeBriefDto {
  employeeId: number;
  fullName: string;
}

export interface OrderTicketDto {
  showtimeId: number;
  movieId: number;
  movieTitle: string;
  posterUrl?: string | null;
  roomName: string;
  startTime: string;
  seatRow: string;
  seatNumber: number;
  ticketTypeId?: number | null;
  qrCodeImage?: string | null;
  isCheckedIn: boolean;
  checkedInAt?: string | null;
}

export interface OrderComboDto {
  comboId?: number | null;
  name?: string | null;
  quantityFulfilled: number;
}

export interface OrderLineDto {
  orderLineId: number;
  lineType: string;
  description?: string | null;
  quantity: number;
  unitListPrice: number;
  lockedUnitPrice: number;
  lineTotal: number;
  paidAmount: number;
  refundedAmount: number;
  refundedQuantity: number;
  ticket?: OrderTicketDto | null;
  combo?: OrderComboDto | null;
}

export interface OrderPriceAdjustmentDto {
  adjustmentId: number;
  type: string;
  amount: number;
  referenceType?: string | null;
  referenceId?: number | null;
  referenceName?: string | null;
}

export interface OrderTransactionDto {
  transactionId: number;
  type: string;
  method: string;
  status: string;
  amount: number;
  gatewayFee?: number | null;
  transactionCode: string;
  gatewayTransactionId?: string | null;
  gatewayResponseCode?: string | null;
  createdAt: string;
  completedAt?: string | null;
  amountReceived?: number | null;
  changeGiven?: number | null;
  originalTransactionId?: number | null;
  needsManualRefund: boolean;
  note?: string | null;
}

export interface OrderPaymentSummaryDto {
  paidAmount: number;
  refundedAmount: number;
  pendingRefundAmount: number;
  netPaidAmount: number;
  outstandingAmount: number;
  overpaidAmount: number;
  isFullyPaid: boolean;
}

export interface OrderDetailDto {
  orderId: number;
  orderCode: string;
  type: string;
  channel: string;
  status: OrderStatus;
  userId?: number | null;
  customerName?: string | null;
  customerPhone?: string | null;
  createdAt: string;
  expiresAt?: string | null;
  createdByEmployee?: OrderEmployeeBriefDto | null;
  originalOrderId?: number | null;
  totalAmount: number;
  discountAmount: number;
  finalAmount: number;
  payment: OrderPaymentSummaryDto;
  cancelReason?: string | null;
  cancelledBy?: string | null;
  cancelledAt?: string | null;
  lines: OrderLineDto[];
  priceAdjustments: OrderPriceAdjustmentDto[];
  transactions: OrderTransactionDto[];
}

export interface PricedSeat {
  seatRow: string;
  seatNumber: number;
  price: number;
}

export interface OrderComboLine {
  name: string;
  quantity: number;
  unitPrice: number;
}
