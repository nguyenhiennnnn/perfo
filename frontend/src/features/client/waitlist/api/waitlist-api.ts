import { apiDelete, apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { RegisterWaitlistRequest, WaitlistEntryDto } from "../types";

export const waitlistApi = {
  getMine: (showtimeId: number) =>
    apiGet<ApiResponse<WaitlistEntryDto>>(ENDPOINTS.waitlist.byShowtime(showtimeId)),

  register: (showtimeId: number, body: RegisterWaitlistRequest) =>
    apiPost<ApiResponse<WaitlistEntryDto>>(ENDPOINTS.waitlist.byShowtime(showtimeId), body),

  withdraw: (showtimeId: number) =>
    apiDelete<ApiResponse<WaitlistEntryDto>>(ENDPOINTS.waitlist.byShowtime(showtimeId)),
};
