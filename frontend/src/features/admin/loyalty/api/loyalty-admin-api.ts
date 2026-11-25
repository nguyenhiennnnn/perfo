import { apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AdminLoyaltyChallengeDto,
  AdminLoyaltyRewardDto,
  DeleteLoyaltyChallengeResponse,
  DeleteLoyaltyRewardResponse,
  LoyaltyChallengeListParams,
  LoyaltyRewardListParams,
  UpsertLoyaltyChallengeRequest,
  UpsertLoyaltyRewardRequest,
} from "../types";

const routes = ENDPOINTS.admin.loyalty;

export const loyaltyAdminApi = {
  getRewards: (params?: LoyaltyRewardListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminLoyaltyRewardDto>>>(routes.rewards, { params }),

  createReward: (body: UpsertLoyaltyRewardRequest) =>
    apiPost<ApiResponse<AdminLoyaltyRewardDto>>(routes.rewards, body),

  updateReward: (id: number, body: UpsertLoyaltyRewardRequest) =>
    apiPut<ApiResponse<AdminLoyaltyRewardDto>>(routes.rewardById(id), body),

  deleteReward: (id: number) =>
    apiDelete<ApiResponse<DeleteLoyaltyRewardResponse>>(routes.rewardById(id)),

  getChallenges: (params?: LoyaltyChallengeListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminLoyaltyChallengeDto>>>(routes.challenges, {
      params,
    }),

  createChallenge: (body: UpsertLoyaltyChallengeRequest) =>
    apiPost<ApiResponse<AdminLoyaltyChallengeDto>>(routes.challenges, body),

  updateChallenge: (id: number, body: UpsertLoyaltyChallengeRequest) =>
    apiPut<ApiResponse<AdminLoyaltyChallengeDto>>(routes.challengeById(id), body),

  deleteChallenge: (id: number) =>
    apiDelete<ApiResponse<DeleteLoyaltyChallengeResponse>>(routes.challengeById(id)),
};
