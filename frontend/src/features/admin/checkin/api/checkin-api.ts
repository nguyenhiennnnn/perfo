import { apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { CheckinRequest, CheckinResponse } from "../types";

export const checkinApi = {
  checkin: (body: CheckinRequest) =>
    apiPost<ApiResponse<CheckinResponse>>(ENDPOINTS.admin.bookings.checkin, body),
};
