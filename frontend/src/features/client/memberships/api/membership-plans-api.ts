import { apiDelete, apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { HoldOrderResponse } from "@/features/client/orders/types";
import type {
  MembershipFreeTicketBalanceDto,
  MembershipPlanDto,
  MembershipSubscriptionDto,
  OrderFreeTicketResponse,
} from "../types";

export const membershipPlansApi = {
  getPlans: () => apiGet<ApiResponse<MembershipPlanDto[]>>(ENDPOINTS.membershipPlans.root),

  purchase: (planId: number) =>
    apiPost<ApiResponse<HoldOrderResponse>>(ENDPOINTS.membershipPlans.purchase(planId)),

  getMySubscriptions: () =>
    apiGet<ApiResponse<MembershipSubscriptionDto[]>>(ENDPOINTS.membershipPlans.mySubscriptions),

  getMyFreeTickets: () =>
    apiGet<ApiResponse<MembershipFreeTicketBalanceDto>>(ENDPOINTS.membershipPlans.myFreeTickets),

  applyFreeTicketToOrder: (orderId: number, orderLineId: number) =>
    apiPost<ApiResponse<OrderFreeTicketResponse>>(ENDPOINTS.orders.freeTicket(orderId), {
      orderLineId,
    }),

  removeFreeTicketFromOrder: (orderId: number, orderLineId: number) =>
    apiDelete<ApiResponse<OrderFreeTicketResponse>>(
      ENDPOINTS.orders.freeTicketByLine(orderId, orderLineId),
    ),
};
