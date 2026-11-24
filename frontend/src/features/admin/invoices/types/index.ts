import type { InvoiceRequestStatus } from "@/types/enum";

export interface InvoiceRequestListParams {
  page: number;
  pageSize: number;
  status?: InvoiceRequestStatus | string;
  search?: string;
  needsAdjustment?: boolean;
}

export interface RejectInvoiceRequestBody {
  reason: string;
}

export interface ResolveInvoiceAdjustmentBody {
  note?: string | null;
}
