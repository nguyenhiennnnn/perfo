export interface MyPromotionDto {
  promotionId: number;
  code: string;
  name: string;
  description?: string | null;
  discountType: string;
  discountValue: number;
  maxDiscountAmount?: number | null;
  scope: string;
  minOrderAmount?: number | null;
  startsAt?: string | null;
  endsAt?: string | null;
  channel?: string | null;
}
