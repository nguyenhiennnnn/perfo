import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { invoicesApi } from "../api/invoices-api";
import type {
  CounterInvoiceRequestRequest,
  CreateInvoiceRequestRequest,
  UpsertInvoiceProfileRequest,
} from "../types";

export function useInvoiceProfiles() {
  return useQuery({
    queryKey: queryKeys.invoices.profiles(),
    queryFn: () => invoicesApi.getMyProfiles(),
    select: (res) => res.data,
  });
}

export function useCreateInvoiceProfile() {
  return useMutation({
    meta: { invalidates: "invoiceProfileChanged" },
    mutationFn: (body: UpsertInvoiceProfileRequest) => invoicesApi.createProfile(body),
  });
}

export function useUpdateInvoiceProfile(profileId: number) {
  return useMutation({
    meta: { invalidates: "invoiceProfileChanged" },
    mutationFn: (body: UpsertInvoiceProfileRequest) => invoicesApi.updateProfile(profileId, body),
  });
}

export function useDeleteInvoiceProfile() {
  return useMutation({
    meta: { invalidates: "invoiceProfileChanged" },
    mutationFn: (profileId: number) => invoicesApi.deleteProfile(profileId),
  });
}

export function useOrderInvoiceRequests(orderId: number, enabled = true) {
  return useQuery({
    queryKey: queryKeys.invoices.orderRequests(orderId),
    queryFn: () => invoicesApi.getOrderRequests(orderId),
    enabled: enabled && orderId > 0,
    select: (res) => res.data,
  });
}

export function useCreateInvoiceRequest(orderId: number) {
  return useMutation({
    meta: { invalidates: "invoiceRequestChanged" },
    mutationFn: (body: CreateInvoiceRequestRequest) => invoicesApi.createRequest(orderId, body),
  });
}

export function useCreateCounterInvoiceRequest(orderId: number) {
  return useMutation({
    meta: { invalidates: "invoiceRequestChanged" },
    mutationFn: (body: CounterInvoiceRequestRequest) =>
      invoicesApi.createCounterRequest(orderId, body),
  });
}
