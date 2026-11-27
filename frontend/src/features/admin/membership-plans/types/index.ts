import type { CancelOrderRefundDto } from "@/features/client/orders/types";
import type {
  MembershipPlanPeriod,
  MembershipRefundReason,
  MembershipSubscriptionStatus,
} from "@/types/enum";

export interface AdminMembershipPlanDto {
  membershipPlanId: number;
  name: string;
  description: string | null;
  period: MembershipPlanPeriod;
  durationMonths: number;
  price: number;
  ticketDiscountPercent: number;
  freeTicketsPerMonth: number;
  comboDiscountPercent: number;
  earlyAccess: boolean;
  stackableWithPromotions: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  freeTicketConversionValue: number;
}

export interface UpsertMembershipPlanRequest {
  name: string;
  description: string | null;
  period: MembershipPlanPeriod;
  price: number;
  ticketDiscountPercent: number;
  freeTicketsPerMonth: number;
  comboDiscountPercent: number;
  earlyAccess: boolean;
  stackableWithPromotions: boolean;
  isActive: boolean;
  freeTicketConversionValue: number;
}

export interface DeleteMembershipPlanResponse {
  membershipPlanId: number;
  deleted: boolean;
}

export interface MembershipPlanListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export interface AdminMembershipSubscriptionDto {
  membershipSubscriptionId: number;
  userId: number;
  customerName: string;
  customerEmail: string;
  customerPhone: string | null;
  membershipPlanId: number;
  planName: string;
  orderId: number;
  orderCode: string;
  planPrice: number;
  validFrom: string;
  validTo: string;
  isCurrent: boolean;
  status: MembershipSubscriptionStatus;
  refundedAt: string | null;
  refundReason: MembershipRefundReason | null;
  stoppedRevenueAmount: number;
  createdAt: string;
}

export interface MembershipSubscriptionListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  planId?: number;
}

export interface RefundMembershipSubscriptionRequest {
  reason: MembershipRefundReason;
  note: string;
}

export interface MembershipRefundResultDto {
  membershipSubscriptionId: number;
  orderId: number;
  orderCode: string;
  orderCancelled: boolean;
  stoppedRevenueAmount: number;
  stoppedMonths: number;
  revokedFreeTickets: number;
  totalRefunded: number;
  refundStatus: "None" | "Refunded" | "Pending";
  refunds: CancelOrderRefundDto[];
}
