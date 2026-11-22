import { useMutation } from "@tanstack/react-query";
import { checkinApi } from "../api/checkin-api";
import type { CheckinRequest } from "../types";

export function useCheckin() {
  return useMutation({
    meta: { invalidates: "bookingChanged", skipErrorToast: true },
    mutationFn: (body: CheckinRequest) => checkinApi.checkin(body),
  });
}
