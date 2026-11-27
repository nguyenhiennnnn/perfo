import { useMutation } from "@tanstack/react-query";

import { eventGiftHandoverApi } from "../api/event-gift-handover-api";

export function useEventGiftLookup() {
  return useMutation({
    meta: { skipErrorToast: true },
    mutationFn: (qrCode: string) => eventGiftHandoverApi.lookup(qrCode),
  });
}

export function useEventGiftHandOver() {
  return useMutation({
    meta: { invalidates: "showtimeEventChanged", skipSuccessToast: true },
    mutationFn: (qrCode: string) => eventGiftHandoverApi.handOver(qrCode),
  });
}
