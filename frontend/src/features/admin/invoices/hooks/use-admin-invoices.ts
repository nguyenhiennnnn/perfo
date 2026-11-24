import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { invoicesAdminApi } from "../api/invoices-admin-api";
import type {
  InvoiceRequestListParams,
  RejectInvoiceRequestBody,
  ResolveInvoiceAdjustmentBody,
} from "../types";

export function useAdminInvoiceRequests(params: InvoiceRequestListParams) {
  return useQuery({
    queryKey: queryKeys.adminInvoiceRequests.list(params),
    queryFn: () => invoicesAdminApi.getRequests(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useIssueInvoice(id: number) {
  return useMutation({
    meta: { invalidates: "invoiceRequestChanged" },
    mutationFn: (body: { invoiceNumber: string; file: File | null }) =>
      invoicesAdminApi.issue(id, body.invoiceNumber, body.file),
  });
}

export function useReplaceInvoiceFile(id: number) {
  return useMutation({
    meta: { invalidates: "invoiceRequestChanged" },
    mutationFn: (file: File) => invoicesAdminApi.replaceFile(id, file),
  });
}

export function useRejectInvoice(id: number) {
  return useMutation({
    meta: { invalidates: "invoiceRequestChanged" },
    mutationFn: (body: RejectInvoiceRequestBody) => invoicesAdminApi.reject(id, body),
  });
}

export function useResolveInvoiceAdjustment(id: number) {
  return useMutation({
    meta: { invalidates: "invoiceRequestChanged" },
    mutationFn: (body: ResolveInvoiceAdjustmentBody) => invoicesAdminApi.resolveAdjustment(id, body),
  });
}
