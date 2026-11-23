import { apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  BulkPersonalCodesRequest,
  BulkPersonalCodesResponse,
  DeletePromotionResponse,
  PromotionDto,
  PromotionListParams,
  PromotionRedemptionDto,
  PromotionRedemptionListParams,
  PromotionReportDto,
  PromotionReportListDto,
  PromotionReportListParams,
  PromotionReportRangeParams,
  UpsertPromotionRequest,
} from "../types";

const routes = ENDPOINTS.admin.promotions;

export const promotionsAdminApi = {
  getAll: (params?: PromotionListParams) =>
    apiGet<ApiResponse<OffsetPageResult<PromotionDto>>>(routes.root, { params }),

  create: (body: UpsertPromotionRequest) =>
    apiPost<ApiResponse<PromotionDto>>(routes.root, body),

  update: (id: number, body: UpsertPromotionRequest) =>
    apiPut<ApiResponse<PromotionDto>>(routes.byId(id), body),

  pause: (id: number) => apiPost<ApiResponse<PromotionDto>>(routes.pause(id)),

  resume: (id: number) => apiPost<ApiResponse<PromotionDto>>(routes.resume(id)),

  delete: (id: number) => apiDelete<ApiResponse<DeletePromotionResponse>>(routes.byId(id)),

  createPersonalCodes: (body: BulkPersonalCodesRequest) =>
    apiPost<ApiResponse<BulkPersonalCodesResponse>>(routes.personalCodesBulk, body),

  getById: (id: number) => apiGet<ApiResponse<PromotionDto>>(routes.byId(id)),

  getRedemptions: (id: number, params?: PromotionRedemptionListParams) =>
    apiGet<ApiResponse<OffsetPageResult<PromotionRedemptionDto>>>(routes.redemptions(id), {
      params,
    }),

  getReports: (params?: PromotionReportListParams) =>
    apiGet<ApiResponse<PromotionReportListDto>>(routes.reports, { params }),

  getReport: (id: number, params?: PromotionReportRangeParams) =>
    apiGet<ApiResponse<PromotionReportDto>>(routes.report(id), { params }),
};
