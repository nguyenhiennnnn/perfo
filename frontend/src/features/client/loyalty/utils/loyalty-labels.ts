import { formatCurrency, formatDate, formatPoints } from "@/lib/formatters";
import {
  LoyaltyChallengeConditionType,
  LoyaltyChallengeRewardType,
  LoyaltyLotStatus,
  LoyaltyLotType,
  LoyaltyRewardType,
  PromotionDiscountType,
} from "@/types/enum";
import type { LoyaltyRewardCatalogItemDto, MyLoyaltyChallengeDto } from "../types";

export const LOT_TYPE_LABELS: Record<LoyaltyLotType, string> = {
  [LoyaltyLotType.Earn]: "Tích điểm từ đơn hàng",
  [LoyaltyLotType.Bonus]: "Điểm thưởng",
  [LoyaltyLotType.Adjustment]: "Điều chỉnh điểm",
};

export const LOT_STATUS_LABELS: Record<LoyaltyLotStatus, string> = {
  [LoyaltyLotStatus.Active]: "Còn hiệu lực",
  [LoyaltyLotStatus.Depleted]: "Đã dùng hết",
  [LoyaltyLotStatus.Expired]: "Đã hết hạn",
};

export const LOT_STATUS_VARIANTS = {
  [LoyaltyLotStatus.Active]: "success",
  [LoyaltyLotStatus.Depleted]: "secondary",
  [LoyaltyLotStatus.Expired]: "destructive",
} as const;

export const describeRewardBenefit = (reward: LoyaltyRewardCatalogItemDto) => {
  switch (reward.type) {
    case LoyaltyRewardType.FreeTicket: {
      const cap = reward.maxDiscountAmount
        ? `, tối đa ${formatCurrency(reward.maxDiscountAmount)}`
        : "";
      return `Miễn phí tiền vé${cap}`;
    }
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

export const describeRewardValidity = (reward: LoyaltyRewardCatalogItemDto) =>
  reward.validTo ? `Đổi được đến ${formatDate(reward.validTo)}` : null;

export const describeCodeValidity = (days: number) =>
  `Mã dùng được trong ${days} ngày kể từ lúc đổi`;

export const CODE_NO_REFUND_NOTICE =
  "Mã hết hạn mà chưa dùng sẽ không được hoàn lại điểm.";

export const describeChallengeCondition = (challenge: MyLoyaltyChallengeDto) =>
  challenge.conditionType === LoyaltyChallengeConditionType.DistinctMovies
    ? `Xem ${challenge.targetCount} phim khác nhau`
    : `Xem ${challenge.targetCount} suất chiếu`;

export const describeChallengeReward = (challenge: MyLoyaltyChallengeDto) =>
  challenge.rewardType === LoyaltyChallengeRewardType.Points
    ? `Thưởng ${formatPoints(challenge.rewardPoints ?? 0)} điểm`
    : `Thưởng quà: ${challenge.rewardGiftName ?? "quà tặng"}`;
