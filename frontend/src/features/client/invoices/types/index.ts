import type { InvoiceRequestStatus } from "@/types/enum";

export interface InvoiceProfileDto {
  invoiceProfileId: number;
  taxCode: string;
  companyName: string;
  address: string;
  email: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UpsertInvoiceProfileRequest {
  taxCode: string;
  companyName: string;
  address: string;
  email: string;
}

export interface DeleteInvoiceProfileResponse {
  invoiceProfileId: number;
  deleted: boolean;
}

export interface CreateInvoiceRequestRequest {
  invoiceProfileId: number;
}

export type CounterInvoiceRequestRequest = UpsertInvoiceProfileRequest;

export interface InvoiceRequestDto {
  invoiceRequestId: number;
  orderId: number;
  orderCode: string;
  invoiceProfileId?: number | null;
  taxCode: string;
  companyName: string;
  address: string;
  email: string;
  status: InvoiceRequestStatus;
  rejectReason?: string | null;
  invoiceNumber?: string | null;
  hasFile: boolean;
  createdAt: string;
  processedAt?: string | null;
  needsAdjustment: boolean;
  adjustmentFlaggedAt?: string | null;
  adjustmentRefundAmount: number;
  adjustmentResolvedAt?: string | null;
  adjustmentNote?: string | null;
}
