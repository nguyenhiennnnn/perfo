import { apiGet, apiPost, apiPut } from "@/api/axios";
import type { ApiResponse, CursorPageResult } from "@/types/api";
import type {
  UserProfileDto,
  UpdateProfileRequest,
  UserMyReviewDto,
  MyReviewListParams,
  CommunicationPreferencesDto,
  UpdateCommunicationPreferencesRequest,
} from "@/features/client/account/types";
import type { ChangePasswordRequest, MessageResponse } from "@/features/auth/types";
import { ENDPOINTS } from "@/api/endpoints";

export const accountApi = {
  getMyProfile: () =>
    apiGet<ApiResponse<UserProfileDto>>(ENDPOINTS.users.me),

  updateMyProfile: (body: UpdateProfileRequest) =>
    apiPut<ApiResponse<UserProfileDto>>(ENDPOINTS.users.me, body),

  getMyReviews: (params?: MyReviewListParams) =>
    apiGet<ApiResponse<CursorPageResult<UserMyReviewDto>>>(ENDPOINTS.users.myReviews, { params }),

  getMyPreferences: () =>
    apiGet<ApiResponse<CommunicationPreferencesDto>>(ENDPOINTS.users.preferences),

  updateMyPreferences: (body: UpdateCommunicationPreferencesRequest) =>
    apiPut<ApiResponse<CommunicationPreferencesDto>>(ENDPOINTS.users.preferences, body),

  unsubscribeMarketing: (token: string) =>
    apiPost<ApiResponse<MessageResponse>>(ENDPOINTS.emailPreferences.unsubscribe, { token }),

  changePassword: (body: ChangePasswordRequest) =>
    apiPost<ApiResponse<MessageResponse>>(ENDPOINTS.users.changePassword, body),
};
