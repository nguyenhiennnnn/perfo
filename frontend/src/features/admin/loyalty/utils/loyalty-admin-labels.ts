import type { EnumOption } from "@/lib/enum-options";
import { getBusinessNow, toWallDate } from "@/lib/business-time";
import { formatCurrency, formatPoints } from "@/lib/formatters";
import {
  LoyaltyChallengeConditionType,
  LoyaltyChallengeRewardType,
  LoyaltyRewardType,
  LoyaltyTransactionType,
  PromotionDiscountType,
} from "@/types/enum";
import type { AdminLoyaltyChallengeDto, AdminLoyaltyRewardDto } from "../types";

export const REWARD_NAME_MAX_LENGTH = 150;
export const REWARD_MAX_POINTS_COST = 10_000_000;
export const REWARD_MAX_QUANTITY = 1_000_000;
export const REWARD_MAX_CODE_VALIDITY_DAYS = 3650;
export const REWARD_MAX_AMOUNT = 1_000_000_000;

export const LOYALTY_POINTS_BLOCK_KEY = "loyalty-points";
export const ADJUSTMENT_MAX_POINTS = 1_000_000;
export const ADJUSTMENT_REASON_MAX_LENGTH = 500;

export const CHALLENGE_NAME_MAX_LENGTH = 150;
export const CHALLENGE_DESCRIPTION_MAX_LENGTH = 500;
export const CHALLENGE_MAX_TARGET_COUNT = 100;
export const CHALLENGE_MAX_REWARD_POINTS = 1_000_000;
export const CHALLENGE_MAX_SPAN_DAYS = 366;

export const REWARD_TYPE_OPTIONS: (EnumOption<LoyaltyRewardType> & { hint: string })[] = [
  {
    value: LoyaltyRewardType.FreeTicket,
    label: "Vé miễn phí",
    hint: "Khách nhận mã miễn phí tiền vé, giảm tối đa theo mức bạn đặt.",
  },
  {
    value: LoyaltyRewardType.FreeCombo,
    label: "Combo miễn phí",
    hint: "Khách nhận mã miễn phí đúng một combo đã chọn.",
  },
  {
    value: LoyaltyRewardType.Discount,
    label: "Mã giảm giá",
    hint: "Khách nhận mã giảm theo phần trăm hoặc số tiền cố định cho đơn hàng.",
  },
];

export const DISCOUNT_TYPE_OPTIONS: EnumOption<PromotionDiscountType>[] = [
  { value: PromotionDiscountType.Percent, label: "Giảm theo phần trăm" },
  { value: PromotionDiscountType.FixedAmount, label: "Giảm số tiền cố định" },
];

export const CHALLENGE_CONDITION_OPTIONS: EnumOption<LoyaltyChallengeConditionType>[] = [
  { value: LoyaltyChallengeConditionType.DistinctMovies, label: "Xem số phim khác nhau" },
  { value: LoyaltyChallengeConditionType.Screenings, label: "Xem số suất chiếu" },
];

export const CHALLENGE_REWARD_TYPE_OPTIONS: EnumOption<LoyaltyChallengeRewardType>[] = [
  { value: LoyaltyChallengeRewardType.Points, label: "Thưởng điểm" },
  { value: LoyaltyChallengeRewardType.Gift, label: "Thưởng quà" },
];

export const ACTIVE_FILTER_OPTIONS: EnumOption[] = [
  { value: "true", label: "Đang bật" },
  { value: "false", label: "Tạm tắt" },
];

export const parseActiveFilter = (value: string | null): boolean | undefined =>
  value === "true" ? true : value === "false" ? false : undefined;

export const getRewardTypeLabel = (type: string) =>
  REWARD_TYPE_OPTIONS.find((option) => option.value === type)?.label ?? type;

export const describeAdminRewardBenefit = (reward: AdminLoyaltyRewardDto) => {
  switch (reward.type) {
    case LoyaltyRewardType.FreeTicket:
      return reward.maxDiscountAmount
        ? `Miễn phí tiền vé, tối đa ${formatCurrency(reward.maxDiscountAmount)}`
        : "Miễn phí tiền vé";
    case LoyaltyRewardType.FreeCombo:
      return reward.comboName ? `Miễn phí combo ${reward.comboName}` : "Miễn phí combo";
    default: {
      if (reward.discountValue == null) return "Mã giảm giá";
      if (reward.discountType === PromotionDiscountType.Percent) {
        const cap = reward.maxDiscountAmount
          ? `, tối đa ${formatCurrency(reward.maxDiscountAmount)}`
          : "";
        return `Giảm ${reward.discountValue}% đơn hàng${cap}`;
      }
      return `Giảm ${formatCurrency(reward.discountValue)} đơn hàng`;
    }
  }
};

export const describeAdminChallengeCondition = (challenge: AdminLoyaltyChallengeDto) =>
  challenge.conditionType === LoyaltyChallengeConditionType.DistinctMovies
    ? `Xem ${challenge.targetCount} phim khác nhau`
    : `Xem ${challenge.targetCount} suất chiếu`;

export const describeAdminChallengeReward = (challenge: AdminLoyaltyChallengeDto) =>
  challenge.rewardType === LoyaltyChallengeRewardType.Points
    ? `${formatPoints(challenge.rewardPoints ?? 0)} điểm`
    : `Quà: ${challenge.rewardGiftName ?? "—"}`;

export type RewardAvailability = "inactive" | "soldOut" | "upcoming" | "expired" | "available";

export const deriveRewardAvailability = (
  reward: AdminLoyaltyRewardDto,
  now: Date = getBusinessNow(),
): RewardAvailability => {
  if (!reward.isActive) return "inactive";
  if (reward.remainingQuantity <= 0) return "soldOut";
  const validFrom = toWallDate(reward.validFrom);
  const validTo = toWallDate(reward.validTo);
  if (validFrom && validFrom > now) return "upcoming";
  if (validTo && validTo < now) return "expired";
  return "available";
};

export const REWARD_AVAILABILITY_BADGES: Record<
  RewardAvailability,
  { label: string; variant: "success" | "secondary" | "destructive" | "warning" }
> = {
  available: { label: "Đang đổi được", variant: "success" },
  inactive: { label: "Tạm tắt", variant: "secondary" },
  soldOut: { label: "Hết số lượng", variant: "destructive" },
  upcoming: { label: "Sắp mở", variant: "warning" },
  expired: { label: "Hết hiệu lực", variant: "secondary" },
};

export type ChallengePhase = "inactive" | "upcoming" | "running" | "ended";

export const deriveChallengePhase = (
  challenge: AdminLoyaltyChallengeDto,
  today: string,
): ChallengePhase => {
  if (!challenge.isActive) return "inactive";
  if (challenge.startsOn > today) return "upcoming";
  if (challenge.endsOn < today) return "ended";
  return "running";
};

export const CHALLENGE_PHASE_BADGES: Record<
  ChallengePhase,
  { label: string; variant: "success" | "secondary" | "warning" }
> = {
  running: { label: "Đang diễn ra", variant: "success" },
  upcoming: { label: "Sắp diễn ra", variant: "warning" },
  ended: { label: "Đã kết thúc", variant: "secondary" },
  inactive: { label: "Tạm tắt", variant: "secondary" },
};

export const ADJUSTMENT_DIRECTION = {
  Add: "add",
  Subtract: "subtract",
} as const;
export type AdjustmentDirection = (typeof ADJUSTMENT_DIRECTION)[keyof typeof ADJUSTMENT_DIRECTION];

export const ADJUSTMENT_DIRECTION_OPTIONS: EnumOption<AdjustmentDirection>[] = [
  { value: ADJUSTMENT_DIRECTION.Add, label: "Cộng điểm" },
  { value: ADJUSTMENT_DIRECTION.Subtract, label: "Trừ điểm" },
];

export const TRANSACTION_TYPE_BADGES: Record<
  LoyaltyTransactionType,
  { label: string; variant: "success" | "secondary" | "destructive" | "warning" | "outline" }
> = {
  [LoyaltyTransactionType.Earn]: { label: "Tích từ đơn", variant: "success" },
  [LoyaltyTransactionType.Bonus]: { label: "Thưởng", variant: "success" },
  [LoyaltyTransactionType.Redeem]: { label: "Đổi quà", variant: "warning" },
  [LoyaltyTransactionType.Expire]: { label: "Hết hạn", variant: "secondary" },
  [LoyaltyTransactionType.Revoke]: { label: "Thu hồi", variant: "destructive" },
  [LoyaltyTransactionType.Adjustment]: { label: "Điều chỉnh", variant: "outline" },
};

export const getTransactionTypeBadge = (type: string) =>
  TRANSACTION_TYPE_BADGES[type as LoyaltyTransactionType] ?? {
    label: type,
    variant: "outline" as const,
  };
