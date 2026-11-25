import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, OffsetPageResult } from "@/types/api";
import type {
  AdminGiftCard,
  AdminGiftCardLedgerEntry,
  AdminGiftCardListParams,
  CancelGiftCardRequest,
  CancelGiftCardResponse,
} from "../types";

const routes = ENDPOINTS.admin.giftCards;

export const giftCardsAdminApi = {
  getAll: (params: AdminGiftCardListParams) =>
    apiGet<ApiResponse<OffsetPageResult<AdminGiftCard>>>(routes.root, { params }),

  getHistory: (id: number) =>
    apiGet<ApiResponse<AdminGiftCardLedgerEntry[]>>(routes.history(id)),

  resend: (id: number) => apiPost<ApiResponse<null>>(routes.resend(id)),

  cancel: (id: number, body: CancelGiftCardRequest) =>
    apiPost<ApiResponse<CancelGiftCardResponse>>(routes.cancel(id), body),
};
