import { apiClient, apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type { InvoiceRequestDto } from "@/features/client/invoices/types";
import type {
  InvoiceRequestListParams,
  RejectInvoiceRequestBody,
  ResolveInvoiceAdjustmentBody,
} from "../types";

const MULTIPART = { headers: { "Content-Type": "multipart/form-data" } };

export const invoicesAdminApi = {
  getRequests: (params: InvoiceRequestListParams) =>
    apiGet<ApiResponse<OffsetPageResult<InvoiceRequestDto>>>(
      ENDPOINTS.admin.invoiceRequests.root,
      { params },
    ),

  issue: (id: number, invoiceNumber: string, file: File | null) => {
    const form = new FormData();
    form.append("invoiceNumber", invoiceNumber);
    if (file) form.append("file", file);
    return apiPost<ApiResponse<InvoiceRequestDto>>(
      ENDPOINTS.admin.invoiceRequests.issue(id),
      form,
      MULTIPART,
    );
  },

  replaceFile: async (id: number, file: File) => {
    const form = new FormData();
    form.append("file", file);
    const { data } = await apiClient.put<ApiResponse<InvoiceRequestDto>>(
      ENDPOINTS.admin.invoiceRequests.file(id),
      form,
      MULTIPART,
    );
    return data;
  },

  reject: (id: number, body: RejectInvoiceRequestBody) =>
    apiPost<ApiResponse<InvoiceRequestDto>>(ENDPOINTS.admin.invoiceRequests.reject(id), body),

  resolveAdjustment: (id: number, body: ResolveInvoiceAdjustmentBody) =>
    apiPost<ApiResponse<InvoiceRequestDto>>(
      ENDPOINTS.admin.invoiceRequests.resolveAdjustment(id),
      body,
    ),
};
