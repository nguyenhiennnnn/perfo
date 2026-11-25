import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { giftCardsAdminApi } from "../api/gift-cards-admin-api";
import type { AdminGiftCardListParams, CancelGiftCardRequest } from "../types";

export function useAdminGiftCards(params: AdminGiftCardListParams) {
  return useQuery({
    queryKey: queryKeys.adminGiftCards.list(params),
    queryFn: () => giftCardsAdminApi.getAll(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useAdminGiftCardHistory(id: number) {
  return useQuery({
    queryKey: queryKeys.adminGiftCards.history(id),
    queryFn: () => giftCardsAdminApi.getHistory(id),
    enabled: id > 0,
    select: (res) => res.data,
  });
}

export function useAdminResendGiftCard() {
  return useMutation({
    mutationFn: (id: number) => giftCardsAdminApi.resend(id),
  });
}

export function useAdminCancelGiftCard() {
  return useMutation({
    meta: { invalidates: "adminGiftCardChanged" },
    mutationFn: ({ id, body }: { id: number; body: CancelGiftCardRequest }) =>
      giftCardsAdminApi.cancel(id, body),
  });
}
