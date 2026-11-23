import { apiGet } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { MyPromotionDto } from "../types";

export const promotionsApi = {
  getMyPromotions: () => apiGet<ApiResponse<MyPromotionDto[]>>(ENDPOINTS.users.myPromotions),
};
