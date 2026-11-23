export const CounterPaymentMethod = {
  Cash: "Cash",
  Card: "Card",
} as const;
export type CounterPaymentMethod =
  (typeof CounterPaymentMethod)[keyof typeof CounterPaymentMethod];

export interface CounterComboSelection {
  comboId: number;
  quantity: number;
}

export interface CounterComboOrderRequest {
  combos: CounterComboSelection[];
  userId?: number | null;
  customerName: string;
  customerPhone?: string | null;
}

export interface CounterOrderResponse {
  orderId: number;
  orderCode: string;
  type: string;
  status: string;
  totalAmount: number;
  discountAmount: number;
  finalAmount: number;
  expiresAt: string;
  channel: string;
  createdByEmployeeId?: number | null;
  userId?: number | null;
  customerName?: string | null;
  customerPhone?: string | null;
}

export interface CounterCheckoutRequest {
  paymentMethod: CounterPaymentMethod;
  amount?: number | null;
  amountReceived?: number | null;
}

export interface CounterCheckoutResponse {
  orderId: number;
  orderCode: string;
  status: string;
  paymentTransactionId: number;
  transactionCode: string;
  paymentMethod: string;
  amountPaid: number;
  amountReceived?: number | null;
  change: number;
  totalPaid: number;
  outstandingAmount: number;
  isFullyPaid: boolean;
}
