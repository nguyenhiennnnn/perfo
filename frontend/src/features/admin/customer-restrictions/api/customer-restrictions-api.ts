import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type { CustomerRestrictionDto, CustomerRestrictionListParams } from "../types";

export const customerRestrictionsApi = {
  getRestrictions: (params: CustomerRestrictionListParams) =>
    apiGet<ApiResponse<OffsetPageResult<CustomerRestrictionDto>>>(
      ENDPOINTS.admin.customerRestrictions.root,
      { params },
    ),

  liftRestriction: (id: number) =>
    apiPost<ApiResponse<CustomerRestrictionDto>>(ENDPOINTS.admin.customerRestrictions.lift(id)),
};
