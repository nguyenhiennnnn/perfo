import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  CreateSupportRequestRequest,
  ReplySupportRequestRequest,
  SupportRequestDto,
  SupportRequestListItemDto,
  SupportRequestListParams,
} from "../types";

export const supportApi = {
  getList: (params: SupportRequestListParams) =>
    apiGet<ApiResponse<OffsetPageResult<SupportRequestListItemDto>>>(
      ENDPOINTS.supportRequests.root,
      { params },
    ),

  getById: (requestId: number) =>
    apiGet<ApiResponse<SupportRequestDto>>(ENDPOINTS.supportRequests.byId(requestId)),

  create: (body: CreateSupportRequestRequest) =>
    apiPost<ApiResponse<SupportRequestDto>>(ENDPOINTS.supportRequests.root, body),

  reply: (requestId: number, body: ReplySupportRequestRequest) =>
    apiPost<ApiResponse<SupportRequestDto>>(ENDPOINTS.supportRequests.messages(requestId), body),
};
