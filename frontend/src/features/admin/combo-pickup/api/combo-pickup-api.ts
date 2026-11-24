import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { ComboPickupOrderDto, ComboPickupRequest, ComboPickupResponse } from "../types";

const comboPickups = ENDPOINTS.admin.comboPickups;

export const comboPickupApi = {
  lookup: (code: string) =>
    apiGet<ApiResponse<ComboPickupOrderDto>>(comboPickups.lookup, { params: { code } }),
  pickUp: (body: ComboPickupRequest) =>
    apiPost<ApiResponse<ComboPickupResponse>>(comboPickups.root, body),
};
