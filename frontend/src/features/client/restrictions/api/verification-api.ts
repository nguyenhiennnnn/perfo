import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { ConfirmVerificationRequest, CustomerVerificationStatusDto } from "../types";

export const verificationApi = {
  getStatus: () =>
    apiGet<ApiResponse<CustomerVerificationStatusDto>>(ENDPOINTS.verification.root),

  sendCode: () =>
    apiPost<ApiResponse<CustomerVerificationStatusDto>>(ENDPOINTS.verification.sendCode),

  confirm: (body: ConfirmVerificationRequest) =>
    apiPost<ApiResponse<CustomerVerificationStatusDto>>(ENDPOINTS.verification.confirm, body),
};
