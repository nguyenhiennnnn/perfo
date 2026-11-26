import { z } from "zod";

import { COMBO_DESCRIPTION_MAX_LENGTH, COMBO_NAME_MAX_LENGTH } from "@/config/default-constants";

import {
  BUNDLE_MAX_DAILY_LIMIT,
  BUNDLE_MAX_ITEMS,
  BUNDLE_MAX_ITEM_QUANTITY,
  BUNDLE_MAX_PRICE,
} from "../../utils/bundle-admin-labels";

const INTEGER_PATTERN = /^\d+$/;

const isInteger = (value: string, min: number, max: number) =>
  INTEGER_PATTERN.test(value.trim()) && Number(value) >= min && Number(value) <= max;

export const bundleItemSchema = z.object({
  kind: z.enum(["ticket", "combo"]),
  refId: z.string().nullable(),
  quantity: z.string(),
});

export type BundleItemFormValue = z.infer<typeof bundleItemSchema>;

export const bundleFormSchema = z
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
    price: z.string(),
    items: z.array(bundleItemSchema),
    validFrom: z.string(),
    validTo: z.string(),
    movieIds: z.array(z.number()),
    showtimeIds: z.array(z.number()),
    seatTypeIds: z.array(z.number()),
    daysOfWeek: z.array(z.number()),
    allowedFromTime: z.string(),
    allowedToTime: z.string(),
    dailyLimit: z.string(),
    excludesPromotions: z.boolean(),
    isActive: z.boolean(),
  })
  .superRefine((values, ctx) => {
    const addIssue = (path: (string | number)[], message: string) =>
      ctx.addIssue({ code: "custom", path, message });

    if (!isInteger(values.price, 1, BUNDLE_MAX_PRICE)) {
      addIssue(
        ["price"],
        `Giá gói phải là số nguyên lớn hơn 0 và không quá ${BUNDLE_MAX_PRICE.toLocaleString("vi-VN")}.`,
      );
    }

    if (values.items.length === 0 || values.items.length > BUNDLE_MAX_ITEMS) {
      addIssue(["items"], `Gói cần từ 1 đến ${BUNDLE_MAX_ITEMS} thành phần.`);
    } else if (!values.items.some((item) => item.kind === "ticket")) {
      addIssue(["items"], "Gói phải có ít nhất một thành phần là vé.");
    }

    const seen = new Set<string>();
    values.items.forEach((item, index) => {
      if (!item.refId) {
        addIssue(
          ["items", index, "refId"],
          item.kind === "ticket" ? "Vui lòng chọn loại vé." : "Vui lòng chọn combo.",
        );
      } else {
        const key = `${item.kind}:${item.refId}`;
        if (seen.has(key)) {
          addIssue(["items", index, "refId"], "Thành phần này đã có trong gói.");
        }
        seen.add(key);
      }

      if (!isInteger(item.quantity, 1, BUNDLE_MAX_ITEM_QUANTITY)) {
        addIssue(
          ["items", index, "quantity"],
          `Số lượng phải là số nguyên từ 1 đến ${BUNDLE_MAX_ITEM_QUANTITY}.`,
        );
      }
    });

    if (values.validFrom && values.validTo && values.validTo <= values.validFrom) {
      addIssue(["validTo"], "Thời điểm kết thúc phải sau thời điểm bắt đầu.");
    }

    const hasFrom = Boolean(values.allowedFromTime);
    const hasTo = Boolean(values.allowedToTime);
    if (hasFrom !== hasTo) {
      addIssue(
        [hasFrom ? "allowedToTime" : "allowedFromTime"],
        "Cần nhập cả giờ bắt đầu và giờ kết thúc.",
      );
    } else if (hasFrom && values.allowedFromTime >= values.allowedToTime) {
      addIssue(["allowedToTime"], "Giờ kết thúc phải sau giờ bắt đầu.");
    }

    if (values.dailyLimit.trim() !== "" && !isInteger(values.dailyLimit, 1, BUNDLE_MAX_DAILY_LIMIT)) {
      addIssue(
        ["dailyLimit"],
        `Hạn mức mỗi ngày phải là số nguyên từ 1 đến ${BUNDLE_MAX_DAILY_LIMIT.toLocaleString("vi-VN")}.`,
      );
    }
  });

export type BundleFormValues = z.infer<typeof bundleFormSchema>;
