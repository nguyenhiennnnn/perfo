import { useMutation } from "@tanstack/react-query";
import { comboPickupApi } from "../api/combo-pickup-api";
import type { ComboPickupRequest } from "../types";

export function useComboPickupLookup() {
  return useMutation({
    meta: { skipErrorToast: true },
    mutationFn: (code: string) => comboPickupApi.lookup(code),
  });
}

export function useComboPickupSubmit() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: ComboPickupRequest) => comboPickupApi.pickUp(body),
  });
}
