import { z } from "zod";

import { PromotionDiscountType, PromotionScope, PromotionType } from "@/types/enum";

import {
  PROMOTION_CODE_PATTERN,
  PROMOTION_DESCRIPTION_MAX_LENGTH,
  PROMOTION_MAX_AMOUNT,
  PROMOTION_MAX_BULK_USERS,
  PROMOTION_MAX_PRIORITY,
  PROMOTION_MAX_USAGE_LIMIT,
  PROMOTION_MAX_VALID_DAYS,
  PROMOTION_NAME_MAX_LENGTH,
  PROMOTION_PREFIX_PATTERN,
} from "../../utils/promotion-admin-labels";

const PERCENT_PATTERN = /^\d+(\.\d{1,2})?$/;
const INTEGER_PATTERN = /^\d+$/;

const discountTypes = [PromotionDiscountType.Percent, PromotionDiscountType.FixedAmount] as const;
const scopes = [
  PromotionScope.TicketsOnly,
  PromotionScope.CombosOnly,
  PromotionScope.WholeOrder,
] as const;

const sharedFieldsShape = {
  name: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập tên khuyến mãi.")
    .max(PROMOTION_NAME_MAX_LENGTH, `Tên khuyến mãi tối đa ${PROMOTION_NAME_MAX_LENGTH} ký tự.`),
  description: z
    .string()
    .trim()
    .max(PROMOTION_DESCRIPTION_MAX_LENGTH, `Mô tả tối đa ${PROMOTION_DESCRIPTION_MAX_LENGTH} ký tự.`),
  discountType: z.enum(discountTypes),
  discountValue: z.string(),
  maxDiscountAmount: z.string(),
  scope: z.enum(scopes),
  minOrderAmount: z.string(),
  startsAt: z.string(),
  endsAt: z.string(),
  channel: z.string(),
  movieIds: z.array(z.number()),
  showtimeIds: z.array(z.number()),
  screenTypeIds: z.array(z.number()),
  daysOfWeek: z.array(z.number()),
  membershipTierIds: z.array(z.number()),
  newCustomersOnly: z.boolean(),
  budgetAmount: z.string(),
  priority: z.string(),
  isExclusive: z.boolean(),
  excludeEventShowtimes: z.boolean(),
};

export const promotionSharedSchema = z.object(sharedFieldsShape);
export type PromotionSharedValues = z.infer<typeof promotionSharedSchema>;

type Issue = (path: string, message: string) => void;

const isBlank = (value: string) => value.trim() === "";

const isAmount = (value: string, min: number) =>
  INTEGER_PATTERN.test(value.trim()) &&
  Number(value) >= min &&
  Number(value) <= PROMOTION_MAX_AMOUNT;

const validateSharedFields = (values: PromotionSharedValues, addIssue: Issue) => {
  const discountValue = values.discountValue.trim();

  if (values.discountType === PromotionDiscountType.Percent) {
    const valid =
      PERCENT_PATTERN.test(discountValue) &&
      Number(discountValue) > 0 &&
      Number(discountValue) <= 100;
    if (!valid) {
      addIssue("discountValue", "Phần trăm giảm phải lớn hơn 0 và tối đa 100, tối đa 2 chữ số thập phân.");
    }
    if (!isBlank(values.maxDiscountAmount) && !isAmount(values.maxDiscountAmount, 1)) {
      addIssue(
        "maxDiscountAmount",
        `Mức giảm tối đa phải là số nguyên lớn hơn 0 và không quá ${PROMOTION_MAX_AMOUNT.toLocaleString("vi-VN")}.`,
      );
    }
  } else if (!isAmount(discountValue, 1)) {
    addIssue(
      "discountValue",
      `Số tiền giảm phải là số nguyên lớn hơn 0 và không quá ${PROMOTION_MAX_AMOUNT.toLocaleString("vi-VN")}.`,
    );
  }

  if (!isBlank(values.minOrderAmount) && !isAmount(values.minOrderAmount, 0)) {
    addIssue(
      "minOrderAmount",
      `Giá trị đơn tối thiểu phải là số nguyên từ 0 đến ${PROMOTION_MAX_AMOUNT.toLocaleString("vi-VN")}.`,
    );
  }

  if (!isBlank(values.budgetAmount) && !isAmount(values.budgetAmount, 1)) {
    addIssue(
      "budgetAmount",
      `Ngân sách phải là số nguyên lớn hơn 0 và không quá ${PROMOTION_MAX_AMOUNT.toLocaleString("vi-VN")}.`,
    );
  }

  const priority = values.priority.trim();
  if (
    !INTEGER_PATTERN.test(priority) ||
    Number(priority) > PROMOTION_MAX_PRIORITY
  ) {
    addIssue("priority", `Độ ưu tiên phải là số nguyên từ 0 đến ${PROMOTION_MAX_PRIORITY}.`);
  }

  if (values.startsAt && values.endsAt && values.endsAt <= values.startsAt) {
    addIssue("endsAt", "Thời điểm kết thúc phải sau thời điểm bắt đầu.");
  }
};

const isLimit = (value: string) =>
  INTEGER_PATTERN.test(value.trim()) &&
  Number(value) >= 1 &&
  Number(value) <= PROMOTION_MAX_USAGE_LIMIT;

const limitMessage = (label: string) =>
  `${label} phải là số nguyên từ 1 đến ${PROMOTION_MAX_USAGE_LIMIT.toLocaleString("vi-VN")}.`;

export const promotionFormSchema = z
  .object({
    ...sharedFieldsShape,
    code: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập mã khuyến mãi.")
      .regex(PROMOTION_CODE_PATTERN, "Mã gồm 3 đến 30 ký tự chữ, số, gạch ngang hoặc gạch dưới."),
    type: z.enum([PromotionType.ManualCode, PromotionType.AutoApply, PromotionType.PersonalCode]),
    totalUsageLimit: z.string(),
    perCustomerLimit: z.string(),
    assignedUserId: z.number().nullable(),
    isActive: z.boolean(),
  })
  .superRefine((values, ctx) => {
    const addIssue: Issue = (path, message) => ctx.addIssue({ code: "custom", path: [path], message });
    validateSharedFields(values, addIssue);

    if (values.type === PromotionType.PersonalCode) {
      if (values.assignedUserId === null) {
        addIssue("assignedUserId", "Vui lòng chọn khách hàng được cấp mã.");
      }
      return;
    }

    if (!isBlank(values.totalUsageLimit) && !isLimit(values.totalUsageLimit)) {
      addIssue("totalUsageLimit", limitMessage("Tổng số lượt dùng"));
    }
    if (!isBlank(values.perCustomerLimit) && !isLimit(values.perCustomerLimit)) {
      addIssue("perCustomerLimit", limitMessage("Số lượt tối đa mỗi khách"));
    }
  });

export type PromotionFormValues = z.infer<typeof promotionFormSchema>;

export const bulkPersonalCodeFormSchema = z
  .object({
    ...sharedFieldsShape,
    codePrefix: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tiền tố mã.")
      .regex(PROMOTION_PREFIX_PATTERN, "Tiền tố gồm 2 đến 12 ký tự chữ hoặc số."),
    userIds: z
      .array(z.number())
      .min(1, "Vui lòng chọn ít nhất 1 khách hàng.")
      .max(PROMOTION_MAX_BULK_USERS, `Chỉ được chọn tối đa ${PROMOTION_MAX_BULK_USERS} khách hàng.`),
    validDays: z.string(),
  })
  .superRefine((values, ctx) => {
    const addIssue: Issue = (path, message) => ctx.addIssue({ code: "custom", path: [path], message });
    validateSharedFields(values, addIssue);

    const validDays = values.validDays.trim();
    if (
      validDays !== "" &&
      (!INTEGER_PATTERN.test(validDays) ||
        Number(validDays) < 1 ||
        Number(validDays) > PROMOTION_MAX_VALID_DAYS)
    ) {
      addIssue("validDays", `Số ngày hiệu lực phải là số nguyên từ 1 đến ${PROMOTION_MAX_VALID_DAYS}.`);
    }
  });

export type BulkPersonalCodeFormValues = z.infer<typeof bulkPersonalCodeFormSchema>;
