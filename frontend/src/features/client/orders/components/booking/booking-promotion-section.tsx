import { useMemo } from "react";
import { PromotionCodeBox } from "@/components/common/promotion-code-box";
import { useMyPromotions } from "@/features/client/promotions/hooks/use-my-promotions";
import {
  describePromotionBenefit,
  describePromotionCondition,
} from "@/features/client/promotions/utils/promotion-labels";
import { PriceAdjustmentType } from "@/types/enum";
import { useApplyPromotion, useRemovePromotion } from "../../hooks/use-orders";
import type { OrderDetailDto } from "../../types";

interface BookingPromotionSectionProps {
  order: OrderDetailDto;
}

export const BookingPromotionSection = ({ order }: BookingPromotionSectionProps) => {
  const applyPromotion = useApplyPromotion(order.orderId);
  const removePromotion = useRemovePromotion(order.orderId);

  const applied = order.priceAdjustments.find(
    (adjustment) => adjustment.type === PriceAdjustmentType.Promotion && adjustment.promotionCode,
  );

  const myPromotions = useMyPromotions(!applied);
  const suggestions = useMemo(
    () =>
      (myPromotions.data ?? []).map((promotion) => ({
        code: promotion.code,
        name: promotion.name,
        benefit: describePromotionBenefit(promotion),
        condition: describePromotionCondition(promotion),
      })),
    [myPromotions.data],
  );

  return (
    <PromotionCodeBox
      inputId="booking-promotion-code"
      appliedCode={applied?.promotionCode}
      appliedName={applied?.referenceName}
      discountAmount={applied?.amount}
      onApply={(code) => applyPromotion.mutateAsync({ code })}
      onRemove={() => removePromotion.mutateAsync()}
      suggestions={suggestions}
      suggestionsLoading={myPromotions.isLoading}
    />
  );
};
