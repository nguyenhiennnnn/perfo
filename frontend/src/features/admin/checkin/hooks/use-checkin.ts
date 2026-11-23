import { useMutation } from "@tanstack/react-query";
import { checkinApi } from "../api/checkin-api";
import type { CheckinRequest, CounterSupplementOrderRequest } from "../types";

export function useCheckin() {
  return useMutation({
    meta: { invalidates: "bookingChanged", skipErrorToast: true },
    mutationFn: (body: CheckinRequest) => checkinApi.checkin(body),
  });
}

export function useRecordCardNotPresented() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (orderLineId: number) => checkinApi.recordCardNotPresented(orderLineId),
  });
}

export function useCreateSupplementOrder() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: CounterSupplementOrderRequest) => checkinApi.createSupplementOrder(body),
  });
}
