import { apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AssignSupportRequestBody,
  ChangeSupportRequestStatusBody,
  IssueSupportApologyCodeBody,
  StaffReplySupportRequestBody,
  StaffSupportRequestDto,
  StaffSupportRequestListItemDto,
  StaffSupportRequestListParams,
} from "../types";

const routes = ENDPOINTS.admin.supportRequests;

export const supportStaffApi = {
  getList: (params: StaffSupportRequestListParams) =>
    apiGet<ApiResponse<OffsetPageResult<StaffSupportRequestListItemDto>>>(routes.root, { params }),

  getById: (requestId: number) => apiGet<ApiResponse<StaffSupportRequestDto>>(routes.byId(requestId)),

  assign: (requestId: number, body: AssignSupportRequestBody) =>
    apiPut<ApiResponse<StaffSupportRequestDto>>(routes.assignee(requestId), body),

  changeStatus: (requestId: number, body: ChangeSupportRequestStatusBody) =>
    apiPut<ApiResponse<StaffSupportRequestDto>>(routes.status(requestId), body),

  reply: (requestId: number, body: StaffReplySupportRequestBody) =>
    apiPost<ApiResponse<StaffSupportRequestDto>>(routes.messages(requestId), body),

  issueApologyCode: (requestId: number, body: IssueSupportApologyCodeBody) =>
    apiPost<ApiResponse<StaffSupportRequestDto>>(routes.apologyCode(requestId), body),

  resendTicketEmail: (requestId: number) =>
    apiPost<ApiResponse<StaffSupportRequestDto>>(routes.resendTicketEmail(requestId)),
};
