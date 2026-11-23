import { useAdminOrderDetail } from "@/features/admin/orders/hooks/use-admin-orders";
import type { OrderPromotionResponse } from "@/features/client/orders/types";
import { PriceAdjustmentType } from "@/types/enum";
import type { CounterOrderResponse } from "../types";
import { useCounterApplyPromotion, useCounterRemovePromotion } from "./use-counter-orders";

export function useCounterPromotionFlow(
  order: CounterOrderResponse | null,
  onRepriced: (pricing: OrderPromotionResponse) => void,
) {
  const orderId = order?.orderId ?? 0;
  const detail = useAdminOrderDetail(orderId);
  const apply = useCounterApplyPromotion();
  const remove = useCounterRemovePromotion();

  const applied =
    detail.data?.priceAdjustments.find(
      (adjustment) =>
        adjustment.type === PriceAdjustmentType.Promotion && adjustment.promotionCode,
    ) ?? null;

  const applyPromotion = async (code: string) => {
    if (!order) return;
    const res = await apply.mutateAsync({ orderId: order.orderId, code });
    onRepriced(res.data);
  };

  const removePromotion = async () => {
    if (!order) return;
    const res = await remove.mutateAsync({ orderId: order.orderId });
    onRepriced(res.data);
  };

  return {
    appliedCode: applied?.promotionCode ?? null,
    appliedName: applied?.referenceName ?? null,
    appliedDiscount: applied?.amount ?? 0,
    applyPromotion,
    removePromotion,
  };
}
