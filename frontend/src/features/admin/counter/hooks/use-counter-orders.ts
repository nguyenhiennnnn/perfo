import { useMutation } from "@tanstack/react-query";
import { ordersAdminApi } from "@/features/admin/orders/api/orders-admin-api";
import type { CancelReason } from "@/types/enum";
import { counterApi } from "../api/counter-api";
import type {
  CounterCheckoutRequest,
  CounterComboOrderRequest,
  CounterHoldRequest,
} from "../types";

export function useCounterHoldOrder() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: CounterHoldRequest) => counterApi.holdOrder(body),
  });
}

export function useCounterComboOrder() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: CounterComboOrderRequest) => counterApi.createComboOrder(body),
  });
}

export function useCounterCheckout() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: ({
      orderId,
      ...body
    }: CounterCheckoutRequest & { orderId: number }) =>
      counterApi.checkout(orderId, body),
  });
}

export function useCounterApplyPromotion() {
  return useMutation({
    meta: { invalidates: "orderPromotionChanged", skipErrorToast: true },
    mutationFn: ({ orderId, code }: { orderId: number; code: string }) =>
      counterApi.applyPromotion(orderId, code),
  });
}

export function useCounterRemovePromotion() {
  return useMutation({
    meta: { invalidates: "orderPromotionChanged", skipErrorToast: true },
    mutationFn: ({ orderId }: { orderId: number }) =>
      counterApi.removePromotion(orderId),
  });
}

export function useCounterApplyGiftCard() {
  return useMutation({
    meta: { invalidates: "orderGiftCardChanged", skipErrorToast: true },
    mutationFn: ({ orderId, code }: { orderId: number; code: string }) =>
      counterApi.applyGiftCard(orderId, code),
  });
}

export function useCounterRemoveGiftCard() {
  return useMutation({
    meta: { invalidates: "orderGiftCardChanged", skipErrorToast: true },
    mutationFn: ({ orderId, giftCardId }: { orderId: number; giftCardId: number }) =>
      counterApi.removeGiftCard(orderId, giftCardId),
  });
}

export function useCounterCancelOrder() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: ({ orderId, reason }: { orderId: number; reason: CancelReason }) =>
      ordersAdminApi.cancelOrder(orderId, reason),
  });
}
