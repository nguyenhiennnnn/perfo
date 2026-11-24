export interface LedgerAdjustment {
  ledgerAdjustmentId: number;
  postingDate: string;
  originalDate: string;
  type: string;
  ticketRevenueDelta: number;
  comboRevenueDelta: number;
  giftCardSoldDelta: number;
  discountDelta: number;
  refundAmountDelta: number;
  cancelledOrdersDelta: number;
  reason: string;
  createdByUserId: number | null;
  createdByName: string | null;
  createdAt: string;
  orderId: number | null;
  paymentTransactionId: number | null;
}

export interface LedgerAdjustmentListParams {
  page?: number;
  pageSize?: number;
  postingFrom?: string;
  postingTo?: string;
  originalDate?: string;
  type?: string;
}

export interface CreateLedgerAdjustmentRequest {
  originalDate: string;
  reason: string;
  ticketRevenueDelta: number;
  comboRevenueDelta: number;
  giftCardSoldDelta: number;
  discountDelta: number;
  refundAmountDelta: number;
  cancelledOrdersDelta: number;
  orderId?: number | null;
  paymentTransactionId?: number | null;
}

export const LEDGER_ADJUSTMENT_REASON_MAX_LENGTH = 500;
export const LEDGER_ADJUSTMENT_MAX_AMOUNT = 100_000_000_000;
export const LEDGER_ADJUSTMENT_MAX_COUNT = 100_000;
