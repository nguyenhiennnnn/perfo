import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  ApproveRefundProposalBody,
  CreateRefundProposalBody,
  ExceptionalRefundProposalDto,
  RefundProposalListParams,
  RejectRefundProposalBody,
} from "../types";

const routes = ENDPOINTS.admin.refundProposals;

export const refundProposalsApi = {
  getList: (params: RefundProposalListParams) =>
    apiGet<ApiResponse<OffsetPageResult<ExceptionalRefundProposalDto>>>(routes.root, { params }),

  propose: (body: CreateRefundProposalBody) =>
    apiPost<ApiResponse<ExceptionalRefundProposalDto>>(routes.root, body),

  approve: (proposalId: number, body: ApproveRefundProposalBody) =>
    apiPost<ApiResponse<ExceptionalRefundProposalDto>>(routes.approve(proposalId), body),

  reject: (proposalId: number, body: RejectRefundProposalBody) =>
    apiPost<ApiResponse<ExceptionalRefundProposalDto>>(routes.reject(proposalId), body),
};
