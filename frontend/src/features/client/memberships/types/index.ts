import type { MembershipPlanPeriod, MembershipSubscriptionStatus } from "@/types/enum";

export interface MembershipPlanDto {
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

export interface MembershipSubscriptionDto {
  membershipSubscriptionId: number;
  membershipPlanId: number;
  planName: string;
  orderId: number;
  validFrom: string;
  validTo: string;
  isCurrent: boolean;
  status: MembershipSubscriptionStatus;
}

export interface MembershipFreeTicketPeriodDto {
  membershipSubscriptionId: number;
  planName: string;
  periodStart: string;
  periodEnd: string;
  granted: number;
  used: number;
  remaining: number;
}

export interface MembershipFreeTicketBalanceDto {
  remaining: number;
  periods: MembershipFreeTicketPeriodDto[];
}

export type MembershipSubscriptionState = "current" | "upcoming" | "expired" | "refunded";
