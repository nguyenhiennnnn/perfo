import { apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  DeletePricingEntityResponse,
  PricePreviewParams,
  PricingRuleDto,
  PricingRuleListParams,
  UpsertPricingRuleRequest,
  ShowtimePriceTableDto,
  SpecialDayDto,
  SpecialDayListParams,
  TicketTypeDto,
  TicketTypeListParams,
  UpsertSpecialDayRequest,
  UpsertTicketTypeRequest,
} from "../types";

export const ticketTypesAdminApi = {
  getAll: (params?: TicketTypeListParams) =>
    apiGet<ApiResponse<OffsetPageResult<TicketTypeDto>>>(ENDPOINTS.admin.ticketTypes.root, {
      params,
    }),
  create: (body: UpsertTicketTypeRequest) =>
    apiPost<ApiResponse<TicketTypeDto>>(ENDPOINTS.admin.ticketTypes.root, body),
  update: (id: number, body: UpsertTicketTypeRequest) =>
    apiPut<ApiResponse<TicketTypeDto>>(ENDPOINTS.admin.ticketTypes.byId(id), body),
  delete: (id: number) =>
    apiDelete<ApiResponse<DeletePricingEntityResponse>>(ENDPOINTS.admin.ticketTypes.byId(id)),
};

export const specialDaysAdminApi = {
  getAll: (params?: SpecialDayListParams) =>
    apiGet<ApiResponse<OffsetPageResult<SpecialDayDto>>>(ENDPOINTS.admin.specialDays.root, {
      params,
    }),
  create: (body: UpsertSpecialDayRequest) =>
    apiPost<ApiResponse<SpecialDayDto>>(ENDPOINTS.admin.specialDays.root, body),
  update: (id: number, body: UpsertSpecialDayRequest) =>
    apiPut<ApiResponse<SpecialDayDto>>(ENDPOINTS.admin.specialDays.byId(id), body),
  delete: (id: number) =>
    apiDelete<ApiResponse<DeletePricingEntityResponse>>(ENDPOINTS.admin.specialDays.byId(id)),
};

export const pricingPreviewAdminApi = {
  getPriceTable: (params: PricePreviewParams) =>
    apiGet<ApiResponse<ShowtimePriceTableDto>>(ENDPOINTS.admin.pricing.preview, { params }),
};

export const pricingRulesAdminApi = {
  getAll: (params?: PricingRuleListParams) =>
    apiGet<ApiResponse<OffsetPageResult<PricingRuleDto>>>(ENDPOINTS.admin.pricingRules.root, {
      params,
    }),
  create: (body: UpsertPricingRuleRequest) =>
    apiPost<ApiResponse<PricingRuleDto>>(ENDPOINTS.admin.pricingRules.root, body),
  update: (id: number, body: UpsertPricingRuleRequest) =>
    apiPut<ApiResponse<PricingRuleDto>>(ENDPOINTS.admin.pricingRules.byId(id), body),
  delete: (id: number) =>
    apiDelete<ApiResponse<DeletePricingEntityResponse>>(ENDPOINTS.admin.pricingRules.byId(id)),
};
