import { addMonths } from "date-fns";
import { formatNumber } from "@/lib/formatters";
import { MembershipPlanPeriod, MembershipSubscriptionStatus } from "@/types/enum";
import type {
  MembershipPlanDto,
  MembershipSubscriptionDto,
  MembershipSubscriptionState,
} from "../types";

export const MEMBERSHIP_EXPIRY_REMINDER_FALLBACK_DAYS = 7;
export const MEMBERSHIP_PLAN_SKELETON_COUNT = 3;

const DAY_MS = 24 * 60 * 60 * 1000;

const PERIOD_LABELS: Record<MembershipPlanPeriod, string> = {
  [MembershipPlanPeriod.Monthly]: "Theo tháng",
  [MembershipPlanPeriod.Quarterly]: "Theo quý",
};

export const getPeriodLabel = (period: string) =>
  PERIOD_LABELS[period as MembershipPlanPeriod] ?? period;

const formatPercentValue = (value: number) =>
  `${formatNumber(value, { maximumFractionDigits: 2 })}%`;

export interface PlanBenefit {
  key: string;
  label: string;
}

export const getPlanBenefits = (plan: MembershipPlanDto): PlanBenefit[] => {
  const benefits: PlanBenefit[] = [];
  if (plan.ticketDiscountPercent > 0) {
    benefits.push({
      key: "ticket-discount",
      label: `Giảm ${formatPercentValue(plan.ticketDiscountPercent)} giá vé xem phim`,
    });
  }
  if (plan.freeTicketsPerMonth > 0) {
    benefits.push({
      key: "free-tickets",
      label: `${plan.freeTicketsPerMonth} vé miễn phí mỗi tháng, không cộng dồn sang tháng sau`,
    });
  }
  if (plan.comboDiscountPercent > 0) {
    benefits.push({
      key: "combo-discount",
      label: `Giảm ${formatPercentValue(plan.comboDiscountPercent)} combo bắp nước`,
    });
  }
  if (plan.earlyAccess) {
    benefits.push({ key: "early-access", label: "Đặt sớm các suất chiếu đặc biệt" });
  }
  benefits.push({
    key: "stackable",
    label: plan.stackableWithPromotions
      ? "Dùng chung được với mã khuyến mãi"
      : "Không dùng chung với mã khuyến mãi",
  });
  return benefits;
};

export const getMonthlyPrice = (plan: MembershipPlanDto) =>
  plan.durationMonths > 0 ? Math.round(plan.price / plan.durationMonths) : plan.price;

export const getSubscriptionState = (
  subscription: MembershipSubscriptionDto,
  now: number,
): MembershipSubscriptionState => {
  if (subscription.status === MembershipSubscriptionStatus.Refunded) return "refunded";
  if (subscription.status === MembershipSubscriptionStatus.Expired) return "expired";
  if (new Date(subscription.validTo).getTime() <= now) return "expired";
  if (subscription.isCurrent || new Date(subscription.validFrom).getTime() <= now) return "current";
  return "upcoming";
};

export interface SubscriptionGroups {
  current: MembershipSubscriptionDto | null;
  upcoming: MembershipSubscriptionDto[];
  history: MembershipSubscriptionDto[];
  renewalStart: Date | null;
}

export const groupSubscriptions = (
  subscriptions: MembershipSubscriptionDto[],
  now: number,
): SubscriptionGroups => {
  let current: MembershipSubscriptionDto | null = null;
  const upcoming: MembershipSubscriptionDto[] = [];
  const history: MembershipSubscriptionDto[] = [];
  let latestEnd: number | null = null;

  for (const subscription of subscriptions) {
    const state = getSubscriptionState(subscription, now);
    if (state === "expired" || state === "refunded") {
      history.push(subscription);
      continue;
    }
    if (state === "current") current = subscription;
    else upcoming.push(subscription);
    const end = new Date(subscription.validTo).getTime();
    if (latestEnd === null || end > latestEnd) latestEnd = end;
  }

  upcoming.sort((a, b) => new Date(a.validFrom).getTime() - new Date(b.validFrom).getTime());
  history.sort((a, b) => new Date(b.validTo).getTime() - new Date(a.validTo).getTime());

  return {
    current,
    upcoming,
    history,
    renewalStart: latestEnd === null ? null : new Date(latestEnd),
  };
};

export const getDaysLeft = (validTo: string, now: number) =>
  Math.max(0, Math.ceil((new Date(validTo).getTime() - now) / DAY_MS));

export const getElapsedPercent = (validFrom: string, validTo: string, now: number) => {
  const start = new Date(validFrom).getTime();
  const end = new Date(validTo).getTime();
  if (end <= start) return 100;
  return Math.min(100, Math.max(0, ((now - start) / (end - start)) * 100));
};

export const getExpectedValidity = (
  plan: MembershipPlanDto,
  renewalStart: Date | null,
  now: number,
) => {
  const from = renewalStart ?? new Date(now);
  return { from, to: addMonths(from, plan.durationMonths), isRenewal: renewalStart !== null };
};
