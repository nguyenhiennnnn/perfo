import { apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  CampaignDto,
  CampaignListParams,
  CampaignReportDto,
  CampaignTestSendResponse,
  DeleteCampaignResponse,
  UpsertCampaignRequest,
} from "../types";

const routes = ENDPOINTS.admin.campaigns;

export const campaignsAdminApi = {
  getAll: (params?: CampaignListParams) =>
    apiGet<ApiResponse<OffsetPageResult<CampaignDto>>>(routes.root, { params }),

  getById: (id: number) => apiGet<ApiResponse<CampaignDto>>(routes.byId(id)),

  getReport: (id: number) => apiGet<ApiResponse<CampaignReportDto>>(routes.report(id)),

  create: (body: UpsertCampaignRequest) => apiPost<ApiResponse<CampaignDto>>(routes.root, body),

  update: (id: number, body: UpsertCampaignRequest) =>
    apiPut<ApiResponse<CampaignDto>>(routes.byId(id), body),

  remove: (id: number) => apiDelete<ApiResponse<DeleteCampaignResponse>>(routes.byId(id)),

  testSend: (id: number) => apiPost<ApiResponse<CampaignTestSendResponse>>(routes.testSend(id)),
};
