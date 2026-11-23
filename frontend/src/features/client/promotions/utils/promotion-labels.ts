import { formatCurrency, formatDate } from "@/lib/formatters";
import { PromotionDiscountType, PromotionScope } from "@/types/enum";
import type { MyPromotionDto } from "../types";

const SCOPE_SUFFIX: Record<string, string> = {
  [PromotionScope.TicketsOnly]: " trên tiền vé",
  [PromotionScope.CombosOnly]: " trên combo",
};

export const describePromotionBenefit = (promotion: MyPromotionDto) => {
  const base =
    promotion.discountType === PromotionDiscountType.Percent
      ? `Giảm ${promotion.discountValue}%`
      : `Giảm ${formatCurrency(promotion.discountValue)}`;
  const cap =
    promotion.discountType === PromotionDiscountType.Percent && promotion.maxDiscountAmount
      ? `, tối đa ${formatCurrency(promotion.maxDiscountAmount)}`
      : "";
  return `${base}${cap}${SCOPE_SUFFIX[promotion.scope] ?? ""}`;
};

export const describePromotionCondition = (promotion: MyPromotionDto) => {
  const parts: string[] = [];
  if (promotion.minOrderAmount) parts.push(`Đơn từ ${formatCurrency(promotion.minOrderAmount)}`);
  if (promotion.endsAt) parts.push(`HSD ${formatDate(promotion.endsAt)}`);
  return parts.join(" · ");
};
