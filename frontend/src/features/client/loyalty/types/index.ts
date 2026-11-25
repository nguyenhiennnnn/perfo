import type {
  LoyaltyChallengeConditionType,
  LoyaltyChallengeRewardType,
  LoyaltyLotStatus,
  LoyaltyLotType,
  LoyaltyRewardType,
  PromotionDiscountType,
} from "@/types/enum";

export interface LoyaltyExpiringLotDto {
  entryId: number;
  points: number;
  expiresAt: string;
}

export interface LoyaltySummaryDto {
  rankPoints: number;
  tierId: number | null;
  tierName: string | null;
  nextTierName: string | null;
  pointsToNextTier: number | null;
  balance: number;
  debt: number;
  expiringWithinDays: number;
  expiringPoints: number;
  expiringLots: LoyaltyExpiringLotDto[];
}

export interface LoyaltyLotDto {
  entryId: number;
  type: LoyaltyLotType;
  points: number;
  redeemedPoints: number;
  expiredPoints: number;
  revokedPoints: number;
  remainingPoints: number;
  orderId: number | null;
  earnedAt: string;
  expiresAt: string;
  status: LoyaltyLotStatus;
}

export interface LoyaltyLotListParams {
  page?: number;
  pageSize?: number;
}

export interface LoyaltyRewardCatalogItemDto {
  rewardId: number;
  name: string;
  type: LoyaltyRewardType;
  pointsCost: number;
  remainingQuantity: number;
  validFrom: string | null;
  validTo: string | null;
  codeValidityDays: number;
  discountType: PromotionDiscountType | null;
  discountValue: number | null;
  maxDiscountAmount: number | null;
  comboId: number | null;
  comboName: string | null;
}

export interface RedeemLoyaltyRewardResponse {
  rewardId: number;
  rewardName: string;
  pointsSpent: number;
  balanceAfter: number;
  promotionId: number;
  code: string;
  codeExpiresAt: string;
  pointsRefundedWhenCodeExpires: boolean;
  notice: string;
}

export interface MyLoyaltyChallengeDto {
  challengeId: number;
  name: string;
  description: string | null;
  startsOn: string;
  endsOn: string;
  conditionType: LoyaltyChallengeConditionType;
  targetCount: number;
  rewardType: LoyaltyChallengeRewardType;
  rewardPoints: number | null;
  rewardGiftName: string | null;
  currentCount: number;
  isCompleted: boolean;
  completedAt: string | null;
  isRewardGranted: boolean;
  isRunning: boolean;
}

export interface ReferralItemDto {
  referredName: string;
  referredAt: string;
  rewardGranted: boolean;
  rewardGrantedAt: string | null;
}

export interface MyReferralsDto {
  referralCode: string | null;
  totalCount: number;
  rewardedCount: number;
  items: ReferralItemDto[];
}
