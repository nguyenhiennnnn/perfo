import { z } from "zod";

import { COMBO_DESCRIPTION_MAX_LENGTH, COMBO_NAME_MAX_LENGTH } from "@/config/default-constants";
import { MembershipPlanPeriod } from "@/types/enum";

import {
  MEMBERSHIP_PLAN_MAX_FREE_TICKETS,
  MEMBERSHIP_PLAN_MAX_PRICE,
  PLAN_PERIOD_MONTHS,
} from "../../utils/membership-plan-admin-labels";

const INTEGER_PATTERN = /^\d+$/;
const PERCENT_PATTERN = /^\d+(\.\d{1,2})?$/;

const isInteger = (value: string, min: number, max: number) =>
  INTEGER_PATTERN.test(value.trim()) && Number(value) >= min && Number(value) <= max;

const isPercent = (value: string) =>
  PERCENT_PATTERN.test(value.trim()) && Number(value) >= 0 && Number(value) <= 100;

export const membershipPlanFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tên gói.")
      .max(COMBO_NAME_MAX_LENGTH, `Tên gói tối đa ${COMBO_NAME_MAX_LENGTH} ký tự.`),
    description: z
      .string()
      .trim()
      .max(COMBO_DESCRIPTION_MAX_LENGTH, `Mô tả tối đa ${COMBO_DESCRIPTION_MAX_LENGTH} ký tự.`),
    period: z.enum([MembershipPlanPeriod.Monthly, MembershipPlanPeriod.Quarterly]),
    price: z.string(),
    ticketDiscountPercent: z.string(),
    freeTicketsPerMonth: z.string(),
    freeTicketConversionValue: z.string(),
    comboDiscountPercent: z.string(),
    earlyAccess: z.boolean(),
    stackableWithPromotions: z.boolean(),
    isActive: z.boolean(),
  })
  .superRefine((values, ctx) => {
    const addIssue = (path: string, message: string) =>
      ctx.addIssue({ code: "custom", path: [path], message });

    const priceValid = isInteger(values.price, 1, MEMBERSHIP_PLAN_MAX_PRICE);
    if (!priceValid) {
      addIssue(
        "price",
        `Giá gói phải là số nguyên lớn hơn 0 và không quá ${MEMBERSHIP_PLAN_MAX_PRICE.toLocaleString("vi-VN")}.`,
      );
    }

    if (!isPercent(values.ticketDiscountPercent)) {
      addIssue("ticketDiscountPercent", "Phần trăm giảm vé phải từ 0 đến 100 (tối đa 2 chữ số thập phân).");
    }
    if (!isPercent(values.comboDiscountPercent)) {
      addIssue("comboDiscountPercent", "Phần trăm giảm combo phải từ 0 đến 100 (tối đa 2 chữ số thập phân).");
    }

    const freeTicketsValid = isInteger(values.freeTicketsPerMonth, 0, MEMBERSHIP_PLAN_MAX_FREE_TICKETS);
    if (!freeTicketsValid) {
      addIssue(
        "freeTicketsPerMonth",
        `Số vé miễn phí mỗi tháng phải là số nguyên từ 0 đến ${MEMBERSHIP_PLAN_MAX_FREE_TICKETS}.`,
      );
    }

    const conversionValid = isInteger(values.freeTicketConversionValue, 0, MEMBERSHIP_PLAN_MAX_PRICE);
    if (!conversionValid) {
      addIssue(
        "freeTicketConversionValue",
        `Giá trị quy đổi phải là số nguyên từ 0 đến ${MEMBERSHIP_PLAN_MAX_PRICE.toLocaleString("vi-VN")}.`,
      );
    } else if (priceValid && freeTicketsValid) {
      const total =
        Number(values.freeTicketConversionValue) *
        Number(values.freeTicketsPerMonth) *
        PLAN_PERIOD_MONTHS[values.period];
      if (total > Number(values.price)) {
        addIssue(
          "freeTicketConversionValue",
          "Tổng giá trị quy đổi của các vé miễn phí không được vượt giá gói.",
        );
      }
    }
  });

export type MembershipPlanFormValues = z.infer<typeof membershipPlanFormSchema>;
