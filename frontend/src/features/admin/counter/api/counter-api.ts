import { apiDelete, apiPost } from "@/api/axios";
import { ENDPOINTS } from "@/api/endpoints";
import type { ApiResponse } from "@/types/api";
import type { OrderGiftCardResponse } from "@/features/client/gift-cards/types";
import type { OrderPromotionResponse } from "@/features/client/orders/types";
import type {
  CounterCheckoutRequest,
  CounterCheckoutResponse,
  CounterComboOrderRequest,
  CounterHoldRequest,
  CounterOrderResponse,
} from "../types";

export const counterApi = {
  holdOrder: (body: CounterHoldRequest) =>
    apiPost<ApiResponse<CounterOrderResponse>>(ENDPOINTS.admin.orders.counterHold, body),

  createComboOrder: (body: CounterComboOrderRequest) =>
    apiPost<ApiResponse<CounterOrderResponse>>(ENDPOINTS.admin.orders.counterComboOrder, body),

  applyPromotion: (orderId: number, code: string) =>
    apiPost<ApiResponse<OrderPromotionResponse>>(
      ENDPOINTS.admin.orders.counterPromotion(orderId),
      { code },
    ),

  removePromotion: (orderId: number) =>
    apiDelete<ApiResponse<OrderPromotionResponse>>(
      ENDPOINTS.admin.orders.counterPromotion(orderId),
    ),

  applyGiftCard: (orderId: number, code: string) =>
    apiPost<ApiResponse<OrderGiftCardResponse>>(
      ENDPOINTS.admin.orders.counterGiftCard(orderId),
      { code },
    ),

  removeGiftCard: (orderId: number, giftCardId: number) =>
    apiDelete<ApiResponse<OrderGiftCardResponse>>(
      ENDPOINTS.admin.orders.counterGiftCardById(orderId, giftCardId),
    ),

  checkout: (orderId: number, body: CounterCheckoutRequest) =>
    apiPost<ApiResponse<CounterCheckoutResponse>>(
      ENDPOINTS.admin.orders.counterCheckout(orderId),
      body,
    ),
};
