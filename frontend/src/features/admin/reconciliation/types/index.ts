export type ReconciliationGateway = "VNPay" | "Momo" | "Stripe";

export interface ReconciliationSessionListParams {
  gateway?: string;
  fromDate?: string;
  toDate?: string;
  page: number;
  pageSize: number;
}

export interface ReconciliationSessionDto {
  sessionId: number;
  gateway: string;
  source: string;
  fileName: string | null;
  periodFrom: string | null;
  periodTo: string | null;
  status: string;
  totalRows: number;
  matchedCount: number;
  missingInSystemCount: number;
  missingAtGatewayCount: number;
  amountMismatchCount: number;
  statusMismatchCount: number;
  paidAtGatewayOrderUnconfirmedCount: number;
  gatewayTotalAmount: number;
  systemTotalAmount: number;
  openItemCount: number;
  resolvedItemCount: number;
  ignoredItemCount: number;
  errorMessage: string | null;
  createdByUserId: number;
  createdByName: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface StripeReconcileParams {
  from: string;
  to: string;
}

export type ReconciliationResolution = "Open" | "Resolved" | "Ignored";

export interface ReconciliationItemListParams {
  sessionId?: number;
  result?: string;
  resolution?: string;
  assignedToUserId?: number;
  gateway?: string;
  page: number;
  pageSize: number;
}

export interface ReconciliationItemDto {
  itemId: number;
  sessionId: number;
  gateway: string;
  gatewayTransactionCode: string;
  gatewayAmount: number | null;
  gatewayFee: number;
  gatewayStatus: string | null;
  gatewayTime: string | null;
  paymentTransactionId: number | null;
  paymentTransactionCode: string | null;
  orderId: number | null;
  orderCode: string | null;
  systemAmount: number | null;
  result: string;
  resolution: ReconciliationResolution;
  note: string | null;
  assignedToUserId: number | null;
  assignedToName: string | null;
  resolvedAt: string | null;
  resolvedByUserId: number | null;
  resolvedByName: string | null;
}

export interface UpdateReconciliationItemRequest {
  note?: string;
  assignedToUserId?: number;
  clearAssignee?: boolean;
  resolution?: ReconciliationResolution;
}
