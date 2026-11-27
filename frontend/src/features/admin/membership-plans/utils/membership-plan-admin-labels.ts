import type { EnumOption } from "@/lib/enum-options";
import { formatNumber } from "@/lib/formatters";
import { MembershipPlanPeriod, MembershipRefundReason, MembershipSubscriptionStatus } from "@/types/enum";

import type { AdminMembershipPlanDto } from "../types";

export const MEMBERSHIP_PLAN_MAX_PRICE = 100_000_000;
export const MEMBERSHIP_PLAN_MAX_FREE_TICKETS = 30;
export const MEMBERSHIP_REFUND_NOTE_MAX_LENGTH = 500;

export const PLAN_PERIOD_OPTIONS: { value: MembershipPlanPeriod; label: string }[] = [
  { value: MembershipPlanPeriod.Monthly, label: "Theo tháng (1 tháng)" },
  { value: MembershipPlanPeriod.Quarterly, label: "Theo quý (3 tháng)" },
];

export const PLAN_PERIOD_MONTHS: Record<MembershipPlanPeriod, number> = {
  [MembershipPlanPeriod.Monthly]: 1,
  [MembershipPlanPeriod.Quarterly]: 3,
};

export const PLAN_ACTIVE_FILTER_OPTIONS: EnumOption[] = [
  { value: "true", label: "Đang bán" },
  { value: "false", label: "Tạm tắt" },
];

export const SUBSCRIPTION_STATUS_FILTER_OPTIONS: EnumOption[] = [
  { value: MembershipSubscriptionStatus.Active, label: "Còn hiệu lực" },
  { value: MembershipSubscriptionStatus.Expired, label: "Đã hết hạn" },
  { value: MembershipSubscriptionStatus.Refunded, label: "Đã hoàn tiền" },
];

export const parseActiveFilter = (value: string | null): boolean | undefined =>
  value === "true" ? true : value === "false" ? false : undefined;

export const parsePlanIdFilter = (value: string | null): number | undefined => {
  const id = Number(value);
  return value !== null && Number.isInteger(id) && id > 0 ? id : undefined;
};

export const formatPercent = (value: number) =>
  `${formatNumber(value, { maximumFractionDigits: 2 })}%`;

export const describePlanBenefits = (plan: AdminMembershipPlanDto): string[] => {
  const benefits: string[] = [];
  if (plan.ticketDiscountPercent > 0) {
    benefits.push(`Giảm ${formatPercent(plan.ticketDiscountPercent)} vé`);
  }
  if (plan.freeTicketsPerMonth > 0) {
    benefits.push(`${plan.freeTicketsPerMonth} vé miễn phí/tháng`);
  }
  if (plan.comboDiscountPercent > 0) {
    benefits.push(`Giảm ${formatPercent(plan.comboDiscountPercent)} combo`);
  }
  if (plan.earlyAccess) benefits.push("Đặt sớm suất đặc biệt");
  return benefits;
};

export const REFUND_REASON_OPTIONS: {
  value: MembershipRefundReason;
  label: string;
  description: string;
}[] = [
  {
    value: MembershipRefundReason.SystemError,
    label: "Lỗi hệ thống",
    description: "Khách mua nhầm hoặc bị trừ tiền do lỗi của hệ thống.",
  },
  {
    value: MembershipRefundReason.AdminException,
    label: "Ngoại lệ do quản trị",
    description: "Hoàn ngoài chính sách theo quyết định của quản trị.",
  },
];

export const getRefundReasonLabel = (reason: string | null) => {
  if (reason === MembershipRefundReason.CustomerRequest) return "Khách yêu cầu";
  return REFUND_REASON_OPTIONS.find((option) => option.value === reason)?.label ?? reason ?? "";
};
