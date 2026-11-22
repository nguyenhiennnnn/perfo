import type { OrderStatus } from "@/types/enum";

export interface AdminPaymentLedgerParams {
  status?: string;
  paymentMethod?: string;
  type?: string;
  needsRefund?: boolean;
  orderCode?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminPaymentListItemDto {
  transactionId: number;
  transactionCode: string;
  orderId: number;
  orderCode: string;
  type: string;
  method: string;
  status: string;
  amount: number;
  gatewayFee: number;
  gatewayTransactionId?: string | null;
  gatewayResponseCode?: string | null;
  amountReceived?: number | null;
  changeGiven?: number | null;
  originalTransactionId?: number | null;
  needsManualRefund: boolean;
  createdAt: string;
  completedAt?: string | null;
}

export interface AdminPaymentOrderBriefDto {
  orderId: number;
  orderCode: string;
  type: string;
  channel: string;
  status: OrderStatus;
  userId?: number | null;
  customerName?: string | null;
  customerPhone?: string | null;
  movieTitle?: string | null;
  startTime?: string | null;
  ticketCount: number;
  finalAmount: number;
  createdAt: string;
  cancelReason?: string | null;
}

export interface AdminPaymentRelatedTransactionDto {
  transactionId: number;
  transactionCode: string;
  type: string;
  method: string;
  status: string;
  amount: number;
  needsManualRefund: boolean;
  createdAt: string;
  completedAt?: string | null;
}

export interface AdminPaymentRefundSummaryDto {
  chargeTransactionId: number;
  chargeAmount: number;
  refundedAmount: number;
  pendingRefundAmount: number;
  refundableAmount: number;
}

export interface AdminPaymentDetailDto {
  transactionId: number;
  transactionCode: string;
  orderId: number;
  type: string;
  method: string;
  status: string;
  amount: number;
  gatewayFee: number;
  gatewayTransactionId?: string | null;
  gatewayResponseCode?: string | null;
  amountReceived?: number | null;
  changeGiven?: number | null;
  needsManualRefund: boolean;
  note?: string | null;
  createdAt: string;
  completedAt?: string | null;
  performedByUserId?: number | null;
  performedByName?: string | null;
  originalTransaction?: AdminPaymentRelatedTransactionDto | null;
  relatedRefunds: AdminPaymentRelatedTransactionDto[];
  refundSummary?: AdminPaymentRefundSummaryDto | null;
  order: AdminPaymentOrderBriefDto;
}
