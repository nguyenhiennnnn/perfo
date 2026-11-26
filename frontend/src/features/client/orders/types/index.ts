import type { ApiResponse } from "@/types/api";
import type { CancelReason, OrderStatus } from "@/types/enum";
import type { BundleSelectionRequest } from "@/features/client/bundles/types";

export interface ComboSelectionRequest {
  comboId: number;
  quantity: number;
}

export interface SeatTicketSelectionRequest {
  showtimeSeatId: number;
  ticketTypeId?: number | null;
  expectedPrice?: number | null;
}

export interface HoldSeatsRequest {
  showtimeId: number;
  customerName: string;
  customerPhone?: string | null;
  seatIds: number[];
  combos?: ComboSelectionRequest[] | null;
  tickets?: SeatTicketSelectionRequest[] | null;
  acceptPriceChange?: boolean;
  bundles?: BundleSelectionRequest[] | null;
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

export interface ComboSupplementRequest {
  combos: ComboSelectionRequest[];
}

export interface OrderComboSuggestionDto {
  comboId: number;
  name: string;
  description?: string | null;
  imageUrl?: string | null;
  suggestedQuantity: number;
  listPrice: number;
  preorderPrice: number;
  label: string;
  matchesTicketCount: boolean;
  purchasedBefore: boolean;
}

export interface OrderComboSuggestionsDto {
  orderId: number;
  canSupplement: boolean;
  ticketCount: number;
  discountPercent: number;
  validUntil?: string | null;
  items: OrderComboSuggestionDto[];
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

export type OrderRefundStatus = "None" | "Pending" | "Refunded";

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
  refundStatus: OrderRefundStatus;
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

export interface OrderPriceAllocationDto {
  orderLineId: number;
  amount: number;
}

export interface OrderPriceAdjustmentDto {
  adjustmentId: number;
  type: string;
  amount: number;
  referenceType?: string | null;
  referenceId?: number | null;
  referenceName?: string | null;
  promotionCode?: string | null;
  allocations?: OrderPriceAllocationDto[] | null;
}

export interface ApplyPromotionRequest {
  code: string;
}

export interface OrderPromotionResponse {
  orderId: number;
  orderCode: string;
  totalAmount: number;
  discountAmount: number;
  finalAmount: number;
  expiresAt: string;
  promotionCode?: string | null;
  promotionName?: string | null;
  promotionDiscount: number;
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
  exchanges?: OrderExchangeHistoryDto[];
}

export interface OrderExchangeHistoryDto {
  orderExchangeId: number;
  exchangeNumber: number;
  originalOrderId: number;
  originalOrderCode: string;
  newOrderId: number;
  newOrderCode: string;
  channel: string;
  exchangedAt: string;
  oldOrderLineId: number;
  newOrderLineId: number;
  oldShowtimeId: number;
  movieTitle: string;
  oldRoomName: string;
  oldStartTime: string;
  oldSeatRow: string;
  oldSeatNumber: number;
  newShowtimeId: number;
  newRoomName: string;
  newStartTime: string;
  newSeatRow: string;
  newSeatNumber: number;
  priceDifference: number;
}

export interface OrderExchangePreviewParams {
  orderLineIds?: number[];
  newShowtimeId?: number;
}

export interface OrderExchangeTicketPreviewDto {
  orderLineId: number;
  ticketTypeId?: number | null;
  paidAmount: number;
  newPrice: number;
  difference: number;
}

export interface OrderExchangePricingDto {
  oldAmount: number;
  newAmount: number;
  difference: number;
  amountDue: number;
  creditAmount: number;
  tickets: OrderExchangeTicketPreviewDto[];
}

export interface OrderExchangeShowtimePreviewDto {
  showtimeId: number;
  startTime: string;
  endTime: string;
  roomId: number;
  roomName: string;
  canExchange: boolean;
  reason?: string | null;
  pricing?: OrderExchangePricingDto | null;
}

export interface OrderExchangePreviewResponse {
  orderId: number;
  movieId: number;
  showtimes: OrderExchangeShowtimePreviewDto[];
}

export interface ExchangeSeatRequest {
  orderLineId: number;
  newShowtimeSeatId: number;
  expectedPrice?: number | null;
}

export interface StartOrderExchangeRequest {
  newShowtimeId: number;
  seats: ExchangeSeatRequest[];
  acceptPriceChange?: boolean;
}

export type OrderExchangeSettlementKind = "None" | "ExchangeCredit" | "CashRefund";

export interface OrderExchangeSettlementResult {
  kind: OrderExchangeSettlementKind;
  amount: number;
  giftCardId?: number | null;
  giftCardCode?: string | null;
  giftCardExpiresAt?: string | null;
  refundTransactionIds: number[];
}

export interface StartOrderExchangeResponse {
  exchangeOrderId: number;
  exchangeOrderCode: string;
  orderId: number;
  status: OrderStatus;
  newShowtimeId: number;
  oldAmount: number;
  newAmount: number;
  difference: number;
  amountDue: number;
  creditAmount: number;
  expiresAt: string;
  tickets: OrderExchangeTicketPreviewDto[];
  settlement?: OrderExchangeSettlementResult | null;
}

export type ExchangeChannel = "customer" | "counter";

export interface OrderExchangeAdapter {
  channel: ExchangeChannel;
  preview: (
    orderId: number,
    params: OrderExchangePreviewParams,
  ) => Promise<ApiResponse<OrderExchangePreviewResponse>>;
  start: (
    orderId: number,
    body: StartOrderExchangeRequest,
  ) => Promise<ApiResponse<StartOrderExchangeResponse>>;
}

export interface PricedSeat {
  seatRow: string;
  seatNumber: number;
  price: number;
  ticketTypeName?: string | null;
}

export interface OrderComboLine {
  name: string;
  quantity: number;
  unitPrice: number;
}
