import type {
  LoyaltyTransactionType,
  LoyaltyChallengeConditionType,
  LoyaltyChallengeRewardType,
  LoyaltyRewardType,
  PromotionDiscountType,
} from "@/types/enum";

export interface AdminLoyaltyRewardDto {
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
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertLoyaltyRewardRequest {
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
  isActive: boolean;
}

export interface LoyaltyRewardListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  type?: string;
  isActive?: boolean;
}

export interface DeleteLoyaltyRewardResponse {
  id: number;
  deleted: boolean;
}

export interface AdminLoyaltyChallengeDto {
  challengeId: number;
  name: string;
  description: string | null;
  startsOn: string;
  endsOn: string;
  conditionType: LoyaltyChallengeConditionType;
  targetCount: number;
  rewardType: LoyaltyChallengeRewardType;
  rewardPoints: number | null;
  rewardGiftId: number | null;
  rewardGiftName: string | null;
  isActive: boolean;
  participantCount: number;
  completedCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertLoyaltyChallengeRequest {
  name: string;
  description: string | null;
  startsOn: string;
  endsOn: string;
  conditionType: LoyaltyChallengeConditionType;
  targetCount: number;
  rewardType: LoyaltyChallengeRewardType;
  rewardPoints: number | null;
  rewardGiftId: number | null;
  isActive: boolean;
}

export interface LoyaltyChallengeListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export interface DeleteLoyaltyChallengeResponse {
  id: number;
  deleted: boolean;
}

export interface LoyaltyCustomerLookupItemDto {
  userId: number;
  fullName: string;
  email: string;
  phone: string | null;
  tierId: number | null;
  tierName: string | null;
  rankPoints: number;
  balance: number;
  debt: number;
}

export type LoyaltyCustomerDetailDto = LoyaltyCustomerLookupItemDto;

export interface LoyaltyTransactionDto {
  transactionId: number;
  type: LoyaltyTransactionType;
  points: number;
  entryId: number | null;
  orderId: number | null;
  actorUserId: number | null;
  actorName: string | null;
  createdAt: string;
}

export interface LoyaltyTransactionListParams {
  page?: number;
  pageSize?: number;
}

export interface AdjustLoyaltyPointsRequest {
  points: number;
  reason: string;
}

export interface LoyaltyAdjustmentResultDto {
  userId: number;
  points: number;
  entryId: number | null;
  balanceBefore: number;
  balanceAfter: number;
  debtBefore: number;
  debtAfter: number;
}
