import type { FinanceReportBlockDto } from "@/features/admin/reports/types";

export interface DailyClosingRevenue {
  ticketRevenue: number;
  comboRevenue: number;
  totalRevenue: number;
  giftCardSold: number;
  giftCardUsed: number;
}

export interface DailyClosingDiscount {
  tierDiscount: number;
  promotionDiscount: number;
  otherDiscount: number;
  totalDiscount: number;
}

export interface DailyClosingRefund {
  paymentRefunded: number;
  ticketRefundAmount: number;
  cancelledOrders: number;
}

export interface DailyClosingMethod {
  channel: string;
  method: string | null;
  ticketRevenue: number;
  comboRevenue: number;
  giftCardSold: number;
  collected: number;
  refunded: number;
  transactionCount: number;
}

export interface DailyClosingOrders {
  pendingOrders: number;
  problemOrders: number;
}

export interface DailyClosingShift {
  counterShiftId: number;
  employeeName: string;
  status: string;
  openingCash: number;
  expectedCash: number;
  countedCash: number | null;
  variance: number | null;
}

export interface DailyClosingCash {
  shiftCount: number;
  openShiftCount: number;
  openingCash: number;
  collected: number;
  refunded: number;
  changeGiven: number;
  expectedCash: number;
  shifts: DailyClosingShift[];
}

export interface DailyClosingSnapshot {
  businessDate: string;
  generatedAt: string;
  revenue: DailyClosingRevenue;
  discounts: DailyClosingDiscount;
  refunds: DailyClosingRefund;
  methods: DailyClosingMethod[];
  orders: DailyClosingOrders;
  cash: DailyClosingCash;
  blocks: FinanceReportBlockDto[];
}

export interface DailyClosingListItem {
  dailyClosingId: number;
  businessDate: string;
  status: string;
  expectedCash: number;
  countedCash: number;
  variance: number;
  note: string | null;
  closedByUserId: number;
  closedByName: string;
  closedAt: string;
}

export interface DailyClosingDto {
  closing: DailyClosingListItem;
  snapshot: DailyClosingSnapshot;
}

export interface CloseDailyClosingRequest {
  countedCash: number;
  note?: string | null;
  overrideBlockers: boolean;
}

export type DailyClosingReportMode = "OriginalDate" | "PostingDate";

export interface DailyClosingListParams {
  page?: number;
  pageSize?: number;
  fromDate?: string;
  toDate?: string;
}

export interface DailyClosingAdjustment {
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
  orderId: number | null;
  paymentTransactionId: number | null;
  createdAt: string;
}

export interface DailyClosingAdjustmentTotals {
  count: number;
  ticketRevenue: number;
  comboRevenue: number;
  giftCardSold: number;
  discount: number;
  refundAmount: number;
  cancelledOrders: number;
}

export interface DailyClosingFigures {
  ticketRevenue: number;
  comboRevenue: number;
  totalRevenue: number;
  giftCardSold: number;
  giftCardUsed: number;
  totalDiscount: number;
  ticketRefundAmount: number;
  cancelledOrders: number;
}

export interface DailyClosingReport {
  mode: DailyClosingReportMode;
  businessDate: string;
  isClosed: boolean;
  closing: DailyClosingListItem | null;
  snapshot: DailyClosingSnapshot;
  base: DailyClosingFigures;
  adjustments: DailyClosingAdjustment[];
  adjustmentTotals: DailyClosingAdjustmentTotals;
  adjusted: DailyClosingFigures;
}

export const DAILY_CLOSING_NOTE_MAX_LENGTH = 500;
export const DAILY_CLOSING_MAX_CASH = 1_000_000_000;
