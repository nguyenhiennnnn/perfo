import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { HoldOrderResponse } from "@/features/client/orders/types";
import type {
  MembershipFreeTicketBalanceDto,
  MembershipPlanDto,
  MembershipSubscriptionDto,
} from "../types";

export const membershipPlansApi = {
  getPlans: () => apiGet<ApiResponse<MembershipPlanDto[]>>(ENDPOINTS.membershipPlans.root),

  purchase: (planId: number) =>
    apiPost<ApiResponse<HoldOrderResponse>>(ENDPOINTS.membershipPlans.purchase(planId)),

  getMySubscriptions: () =>
    apiGet<ApiResponse<MembershipSubscriptionDto[]>>(ENDPOINTS.membershipPlans.mySubscriptions),

  getMyFreeTickets: () =>
    apiGet<ApiResponse<MembershipFreeTicketBalanceDto>>(ENDPOINTS.membershipPlans.myFreeTickets),
};
