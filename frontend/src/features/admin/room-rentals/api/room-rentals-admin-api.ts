import { apiClient, apiDelete, apiGet, apiPost, apiPut } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import { EXPORT_TIMEOUT_MS } from "@/config/default-constants";
import { getFilenameFromContentDisposition, normalizeBlobError } from "@/lib/download";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  ChangeRoomRentalStatusBody,
  RoomRentalAdminDto,
  RoomRentalAdminListItemDto,
  RoomRentalListParams,
  RoomRentalQuotePricingRequest,
  RoomRentalQuoteResultDto,
  RoomRentalQuoteVersionDto,
  RoomRentalQuoteVersionListItemDto,
  RoomRentalRateDto,
  RoomRentalRateListParams,
  UpsertRoomRentalQuoteVersionBody,
  UpsertRoomRentalRateRequest,
} from "../types";

const requests = ENDPOINTS.admin.roomRentalRequests;
const rates = ENDPOINTS.admin.roomRentalRates;

export const roomRentalsAdminApi = {
  getList: (params: RoomRentalListParams) =>
    apiGet<ApiResponse<OffsetPageResult<RoomRentalAdminListItemDto>>>(requests.root, { params }),

  getById: (id: number) => apiGet<ApiResponse<RoomRentalAdminDto>>(requests.byId(id)),

  changeStatus: (id: number, body: ChangeRoomRentalStatusBody) =>
    apiPut<ApiResponse<RoomRentalAdminDto>>(requests.status(id), body),

  getQuotes: (id: number) =>
    apiGet<ApiResponse<RoomRentalQuoteVersionListItemDto[]>>(requests.quotes(id)),

  getQuote: (id: number, version: number) =>
    apiGet<ApiResponse<RoomRentalQuoteVersionDto>>(requests.quoteByVersion(id, version)),

  createQuote: (id: number, body: UpsertRoomRentalQuoteVersionBody) =>
    apiPost<ApiResponse<RoomRentalQuoteVersionDto>>(requests.quotes(id), body),

  sendQuote: (id: number, version: number) =>
    apiPost<ApiResponse<RoomRentalQuoteVersionDto>>(requests.quoteSend(id, version)),

  calculate: (body: RoomRentalQuotePricingRequest) =>
    apiPost<ApiResponse<RoomRentalQuoteResultDto>>(ENDPOINTS.admin.roomRentalQuotes.calculate, body),

  downloadQuotePdf: async (id: number, version: number, fallbackFilename: string) => {
    try {
      const response = await apiClient.get<Blob>(requests.quotePdf(id, version), {
        responseType: "blob",
        timeout: EXPORT_TIMEOUT_MS,
      });
      return {
        blob: response.data,
        filename:
          getFilenameFromContentDisposition(response.headers["content-disposition"]) ??
          fallbackFilename,
      };
    } catch (error) {
      throw await normalizeBlobError(error);
    }
  },
};

export const roomRentalRatesAdminApi = {
  getAll: (params?: RoomRentalRateListParams) =>
    apiGet<ApiResponse<OffsetPageResult<RoomRentalRateDto>>>(rates.root, { params }),

  create: (body: UpsertRoomRentalRateRequest) =>
    apiPost<ApiResponse<RoomRentalRateDto>>(rates.root, body),

  update: (id: number, body: UpsertRoomRentalRateRequest) =>
    apiPut<ApiResponse<RoomRentalRateDto>>(rates.byId(id), body),

  delete: (id: number) => apiDelete<ApiResponse<unknown>>(rates.byId(id)),
};
