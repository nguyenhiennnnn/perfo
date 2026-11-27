import { apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  CustomerSegmentDto,
  CustomerSegmentListParams,
  DeleteCustomerSegmentResponse,
  UpsertCustomerSegmentRequest,
} from "../types";

const routes = ENDPOINTS.admin.customerSegments;

export const customerSegmentsAdminApi = {
  getAll: (params?: CustomerSegmentListParams) =>
    apiGet<ApiResponse<OffsetPageResult<CustomerSegmentDto>>>(routes.root, { params }),

  create: (body: UpsertCustomerSegmentRequest) =>
    apiPost<ApiResponse<CustomerSegmentDto>>(routes.root, body),

  update: (id: number, body: UpsertCustomerSegmentRequest) =>
    apiPut<ApiResponse<CustomerSegmentDto>>(routes.byId(id), body),

  remove: (id: number) => apiDelete<ApiResponse<DeleteCustomerSegmentResponse>>(routes.byId(id)),
};
