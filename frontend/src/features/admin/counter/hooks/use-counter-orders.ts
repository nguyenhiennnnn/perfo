import { useMutation } from "@tanstack/react-query";
import { counterApi } from "../api/counter-api";
import type { CancelOrderRequest } from "@/features/admin/orders/types";
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

export function useCounterCancelOrder() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: ({
      orderId,
      ...body
    }: CancelOrderRequest & { orderId: number }) =>
      counterApi.cancelOrder(orderId, body),
  });
}
