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
