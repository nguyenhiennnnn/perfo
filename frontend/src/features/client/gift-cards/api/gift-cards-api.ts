import { apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, CursorPageResult } from "@/types/api";
import type { HoldOrderResponse } from "@/features/client/orders/types";
import type {
  PurchaseGiftCardRequest,
  PurchasedGiftCardDto,
  PurchasedGiftCardListParams,
} from "../types";

export const giftCardsApi = {
  purchase: (body: PurchaseGiftCardRequest) =>
    apiPost<ApiResponse<HoldOrderResponse>>(ENDPOINTS.giftCards.purchase, body),

  getPurchased: (params: PurchasedGiftCardListParams) =>
    apiGet<ApiResponse<CursorPageResult<PurchasedGiftCardDto>>>(ENDPOINTS.giftCards.purchased, { params }),
};
