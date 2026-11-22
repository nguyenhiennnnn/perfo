export interface CashRefundListParams {
  status?: string;
  orderCode?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface CashRefundListItemDto {
  refundTransactionId: number;
  transactionCode: string;
  orderId: number;
  orderCode: string;
  customerUserId?: number | null;
  customerName: string;
  customerPhone?: string | null;
  isGuest: boolean;
  amount: number;
  showtimeId?: number | null;
  movieTitle?: string | null;
  showtimeStartTime?: string | null;
  status: string;
  createdAt: string;
  refundedAt?: string | null;
  confirmedByUserId?: number | null;
  confirmedByName?: string | null;
}

export interface CashRefundConfirmResultDto {
  refundTransactionId: number;
  orderId: number;
  orderCode: string;
  amount: number;
  status: string;
  refundedAt?: string | null;
  confirmedByUserId?: number | null;
  confirmedByName?: string | null;
}

export const CashRefundStatus = {
  Pending: "Pending",
  Refunded: "Refunded",
  All: "All",
} as const;
export type CashRefundStatus = (typeof CashRefundStatus)[keyof typeof CashRefundStatus];
