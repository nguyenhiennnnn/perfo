import { apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AdminBundleDto,
  BundleListParams,
  DeleteBundleResponse,
  UpsertBundleRequest,
} from "../types";

const routes = ENDPOINTS.admin.bundles;

export const bundlesAdminApi = {
  getAll: (params?: BundleListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminBundleDto>>>(routes.root, { params }),

  create: (body: UpsertBundleRequest) => apiPost<ApiResponse<AdminBundleDto>>(routes.root, body),

  update: (id: number, body: UpsertBundleRequest) =>
    apiPut<ApiResponse<AdminBundleDto>>(routes.byId(id), body),

  delete: (id: number) => apiDelete<ApiResponse<DeleteBundleResponse>>(routes.byId(id)),
};
