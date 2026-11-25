import { z } from "zod";

import { LoyaltyRewardType, PromotionDiscountType } from "@/types/enum";

import {
  REWARD_MAX_AMOUNT,
  REWARD_MAX_CODE_VALIDITY_DAYS,
  REWARD_MAX_POINTS_COST,
  REWARD_MAX_QUANTITY,
  REWARD_NAME_MAX_LENGTH,
} from "../../utils/loyalty-admin-labels";

const INTEGER_PATTERN = /^\d+$/;
const PERCENT_PATTERN = /^\d+(\.\d{1,2})?$/;

const isInteger = (value: string, min: number, max: number) =>
  INTEGER_PATTERN.test(value.trim()) && Number(value) >= min && Number(value) <= max;

const isAmount = (value: string) => isInteger(value, 1, REWARD_MAX_AMOUNT);

const isBlank = (value: string) => value.trim() === "";

export const rewardFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tên quà.")
      .max(REWARD_NAME_MAX_LENGTH, `Tên quà tối đa ${REWARD_NAME_MAX_LENGTH} ký tự.`),
    type: z.enum([
      LoyaltyRewardType.FreeTicket,
      LoyaltyRewardType.FreeCombo,
      LoyaltyRewardType.Discount,
    ]),
    pointsCost: z.string(),
    remainingQuantity: z.string(),
    codeValidityDays: z.string(),
    validFrom: z.string(),
    validTo: z.string(),
    discountType: z.enum([PromotionDiscountType.Percent, PromotionDiscountType.FixedAmount]),
    discountValue: z.string(),
    maxDiscountAmount: z.string(),
    comboId: z.string().nullable(),
    isActive: z.boolean(),
  })
  .superRefine((values, ctx) => {
    const addIssue = (path: string, message: string) =>
      ctx.addIssue({ code: "custom", path: [path], message });

    if (!isInteger(values.pointsCost, 1, REWARD_MAX_POINTS_COST)) {
      addIssue(
        "pointsCost",
        `Số điểm cần phải là số nguyên từ 1 đến ${REWARD_MAX_POINTS_COST.toLocaleString("vi-VN")}.`,
      );
    }

    if (!isInteger(values.remainingQuantity, 0, REWARD_MAX_QUANTITY)) {
      addIssue(
        "remainingQuantity",
        `Số lượng còn phải là số nguyên từ 0 đến ${REWARD_MAX_QUANTITY.toLocaleString("vi-VN")}.`,
      );
    }

    if (!isInteger(values.codeValidityDays, 1, REWARD_MAX_CODE_VALIDITY_DAYS)) {
      addIssue(
        "codeValidityDays",
        `Hạn dùng của mã phải là số nguyên từ 1 đến ${REWARD_MAX_CODE_VALIDITY_DAYS.toLocaleString("vi-VN")} ngày.`,
      );
    }

    if (values.validFrom && values.validTo && values.validTo <= values.validFrom) {
      addIssue("validTo", "Thời điểm kết thúc phải sau thời điểm bắt đầu.");
    }

    const amountMessage = `phải là số nguyên lớn hơn 0 và không quá ${REWARD_MAX_AMOUNT.toLocaleString("vi-VN")}.`;

    if (values.type === LoyaltyRewardType.FreeCombo) {
      if (!values.comboId) addIssue("comboId", "Vui lòng chọn combo tặng khách.");
      return;
    }

    if (values.type === LoyaltyRewardType.FreeTicket) {
      if (!isAmount(values.maxDiscountAmount)) {
        addIssue("maxDiscountAmount", `Mức giảm tối đa ${amountMessage}`);
      }
      return;
    }

    const discountValue = values.discountValue.trim();
    if (values.discountType === PromotionDiscountType.Percent) {
      const valid =
        PERCENT_PATTERN.test(discountValue) &&
        Number(discountValue) > 0 &&
        Number(discountValue) <= 100;
      if (!valid) {
        addIssue(
          "discountValue",
          "Phần trăm giảm phải lớn hơn 0 và tối đa 100, tối đa 2 chữ số thập phân.",
        );
      }
      if (!isBlank(values.maxDiscountAmount) && !isAmount(values.maxDiscountAmount)) {
        addIssue("maxDiscountAmount", `Mức giảm tối đa ${amountMessage}`);
      }
      return;
    }

    if (!isAmount(discountValue)) addIssue("discountValue", `Số tiền giảm ${amountMessage}`);
  });

export type RewardFormValues = z.infer<typeof rewardFormSchema>;
