import { apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type {
  CounterInvoiceRequestRequest,
  CreateInvoiceRequestRequest,
  DeleteInvoiceProfileResponse,
  InvoiceProfileDto,
  InvoiceRequestDto,
  UpsertInvoiceProfileRequest,
} from "../types";

export const invoicesApi = {
  getMyProfiles: () =>
    apiGet<ApiResponse<InvoiceProfileDto[]>>(ENDPOINTS.invoiceProfiles.root),

  createProfile: (body: UpsertInvoiceProfileRequest) =>
    apiPost<ApiResponse<InvoiceProfileDto>>(ENDPOINTS.invoiceProfiles.root, body),

  updateProfile: (profileId: number, body: UpsertInvoiceProfileRequest) =>
    apiPut<ApiResponse<InvoiceProfileDto>>(ENDPOINTS.invoiceProfiles.byId(profileId), body),

  deleteProfile: (profileId: number) =>
    apiDelete<ApiResponse<DeleteInvoiceProfileResponse>>(ENDPOINTS.invoiceProfiles.byId(profileId)),

  getOrderRequests: (orderId: number) =>
    apiGet<ApiResponse<InvoiceRequestDto[]>>(ENDPOINTS.orders.invoiceRequests(orderId)),

  createRequest: (orderId: number, body: CreateInvoiceRequestRequest) =>
    apiPost<ApiResponse<InvoiceRequestDto>>(ENDPOINTS.orders.invoiceRequests(orderId), body),

  createCounterRequest: (orderId: number, body: CounterInvoiceRequestRequest) =>
    apiPost<ApiResponse<InvoiceRequestDto>>(
      ENDPOINTS.admin.orders.counterInvoiceRequest(orderId),
      body,
    ),
};
