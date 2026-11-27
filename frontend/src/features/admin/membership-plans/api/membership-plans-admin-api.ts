import { apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AdminMembershipPlanDto,
  AdminMembershipSubscriptionDto,
  DeleteMembershipPlanResponse,
  MembershipPlanListParams,
  MembershipRefundResultDto,
  MembershipSubscriptionListParams,
  RefundMembershipSubscriptionRequest,
  UpsertMembershipPlanRequest,
} from "../types";

const planRoutes = ENDPOINTS.admin.membershipPlans;
const subscriptionRoutes = ENDPOINTS.admin.membershipSubscriptions;

export const membershipPlansAdminApi = {
  getPlans: (params?: MembershipPlanListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminMembershipPlanDto>>>(planRoutes.root, { params }),

  createPlan: (body: UpsertMembershipPlanRequest) =>
    apiPost<ApiResponse<AdminMembershipPlanDto>>(planRoutes.root, body),

  updatePlan: (id: number, body: UpsertMembershipPlanRequest) =>
    apiPut<ApiResponse<AdminMembershipPlanDto>>(planRoutes.byId(id), body),

  deletePlan: (id: number) =>
    apiDelete<ApiResponse<DeleteMembershipPlanResponse>>(planRoutes.byId(id)),

  getSubscriptions: (params?: MembershipSubscriptionListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminMembershipSubscriptionDto>>>(
      subscriptionRoutes.root,
      { params },
    ),

  refundSubscription: (id: number, body: RefundMembershipSubscriptionRequest) =>
    apiPost<ApiResponse<MembershipRefundResultDto>>(subscriptionRoutes.refund(id), body),
};
