import { useState } from "react";
import { useGiftCardBalance } from "@/features/client/gift-cards/hooks/use-gift-cards";
import type { OrderGiftCardResponse } from "@/features/client/gift-cards/types";
import type { CounterOrderResponse } from "../types";
import { useCounterApplyGiftCard, useCounterRemoveGiftCard } from "./use-counter-orders";

export function useCounterGiftCardFlow(
  order: CounterOrderResponse | null,
  onFullyCovered: () => void,
) {
  const [cover, setCover] = useState<OrderGiftCardResponse | null>(null);
  const balance = useGiftCardBalance();
  const apply = useCounterApplyGiftCard();
  const remove = useCounterRemoveGiftCard();

  const lookup = async (code: string) => (await balance.mutateAsync(code)).data;

  const applyCard = async (code: string) => {
    if (!order) return;
    const res = await apply.mutateAsync({ orderId: order.orderId, code });
    setCover(res.data);
    if (res.data.isFullyCovered) onFullyCovered();
  };

  const removeCard = async (giftCardId: number) => {
    if (!order) return;
    const res = await remove.mutateAsync({ orderId: order.orderId, giftCardId });
    setCover(res.data);
  };

  return {
    cards: cover?.cards ?? [],
    giftAmount: cover?.giftCardAmount ?? 0,
    lookup,
    applyCard,
    removeCard,
    reset: () => setCover(null),
  };
}
