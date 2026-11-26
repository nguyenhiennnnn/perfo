import { apiGet } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { ApplicableBundleDto } from "../types";

export const bundlesApi = {
  getForShowtime: (showtimeId: number) =>
    apiGet<ApiResponse<ApplicableBundleDto[]>>(ENDPOINTS.bundles.root, {
      params: { showtimeId },
    }),
};
