import { useMutation } from "@tanstack/react-query";
import type { CancelReason } from "@/types/enum";
import { counterOrderApi } from "../api/counter-order-api";
import type {
  CounterCheckoutRequest,
  CounterComboOrderRequest,
} from "../types";

export function useCreateCounterComboOrder() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: CounterComboOrderRequest) =>
      counterOrderApi.createComboOrder(body),
  });
}

export function useCounterOrderCheckout() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: ({
      orderId,
      body,
    }: {
      orderId: number;
      body: CounterCheckoutRequest;
    }) => counterOrderApi.checkout(orderId, body),
  });
}

export function useCancelCounterOrder() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: ({
      orderId,
      reason,
    }: {
      orderId: number;
      reason: CancelReason;
    }) => counterOrderApi.cancel(orderId, reason),
  });
}
