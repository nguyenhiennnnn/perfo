import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  LoyaltyLotDto,
  LoyaltyLotListParams,
  LoyaltyRewardCatalogItemDto,
  LoyaltySummaryDto,
  MyLoyaltyChallengeDto,
  MyReferralsDto,
  RedeemLoyaltyRewardResponse,
} from "../types";

export const loyaltyApi = {
  getSummary: () => apiGet<ApiResponse<LoyaltySummaryDto>>(ENDPOINTS.loyalty.summary),

  getLots: (params: LoyaltyLotListParams) =>
    apiGet<ApiResponse<OffsetPageResult<LoyaltyLotDto>>>(ENDPOINTS.loyalty.lots, { params }),

  getRewards: () => apiGet<ApiResponse<LoyaltyRewardCatalogItemDto[]>>(ENDPOINTS.loyalty.rewards),

  getChallenges: () =>
    apiGet<ApiResponse<MyLoyaltyChallengeDto[]>>(ENDPOINTS.loyalty.challenges),

  getReferrals: () => apiGet<ApiResponse<MyReferralsDto>>(ENDPOINTS.loyalty.referrals),

  redeemReward: (rewardId: number) =>
    apiPost<ApiResponse<RedeemLoyaltyRewardResponse>>(ENDPOINTS.loyalty.redeemReward(rewardId)),
};
