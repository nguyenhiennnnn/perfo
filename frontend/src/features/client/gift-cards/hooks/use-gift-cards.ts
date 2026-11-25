import { useInfiniteQuery, useMutation } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { GIFT_CARD_STATUS_POLL_INTERVAL_MS } from "@/config/default-constants";
import { GiftCardStatus } from "@/types/enum";
import { giftCardsApi } from "../api/gift-cards-api";
import type { PurchaseGiftCardRequest, PurchasedGiftCardListParams } from "../types";

export function usePurchaseGiftCard() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: PurchaseGiftCardRequest) => giftCardsApi.purchase(body),
  });
}

export function usePurchasedGiftCards(params: Omit<PurchasedGiftCardListParams, "cursor">) {
  return useInfiniteQuery({
    queryKey: queryKeys.giftCards.purchased(params),
    queryFn: ({ pageParam }) => giftCardsApi.getPurchased({ ...params, cursor: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.data.hasMore ? (lastPage.data.nextCursor ?? undefined) : undefined,
    refetchInterval: (query) =>
      query.state.data?.pages.some((page) =>
        page.data.items.some((card) => card.status === GiftCardStatus.PendingSend),
      )
        ? GIFT_CARD_STATUS_POLL_INTERVAL_MS
        : false,
    select: (data) => ({
      cards: data.pages.flatMap((page) => page.data.items),
      hasMore: data.pages.at(-1)?.data.hasMore ?? false,
    }),
  });
}
