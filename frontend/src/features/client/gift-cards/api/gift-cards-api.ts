import { apiDelete, apiGet, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse, CursorPageResult } from "@/types/api";
import type { HoldOrderResponse } from "@/features/client/orders/types";
import type {
  GiftCardLookupDto,
  OrderGiftCardResponse,
  PurchaseGiftCardRequest,
  PurchasedGiftCardDto,
  PurchasedGiftCardListParams,
} from "../types";

export const giftCardsApi = {
  purchase: (body: PurchaseGiftCardRequest) =>
    apiPost<ApiResponse<HoldOrderResponse>>(ENDPOINTS.giftCards.purchase, body),

  getPurchased: (params: PurchasedGiftCardListParams) =>
    apiGet<ApiResponse<CursorPageResult<PurchasedGiftCardDto>>>(ENDPOINTS.giftCards.purchased, { params }),

  checkBalance: (code: string) =>
    apiPost<ApiResponse<GiftCardLookupDto>>(ENDPOINTS.giftCards.balance, { code }),

  applyToOrder: (orderId: number, code: string) =>
    apiPost<ApiResponse<OrderGiftCardResponse>>(ENDPOINTS.orders.giftCard(orderId), { code }),

  removeFromOrder: (orderId: number, giftCardId: number) =>
    apiDelete<ApiResponse<OrderGiftCardResponse>>(ENDPOINTS.orders.giftCardById(orderId, giftCardId)),
};
