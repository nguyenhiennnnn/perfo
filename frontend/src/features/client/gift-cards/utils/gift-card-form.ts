import { z } from "zod";
import { VALIDATION_MESSAGES } from "@/config/error-messages";
import {
  EMAIL_MAX_LENGTH,
  GIFT_CARD_MESSAGE_MAX_LENGTH,
  GIFT_CARD_RECIPIENT_NAME_MAX_LENGTH,
} from "@/config/default-constants";
import { toBusinessIso } from "@/lib/business-time";
import { formatCurrency } from "@/lib/formatters";
import { getServerNow } from "@/lib/server-clock";
import type { CinemaSettingsDto } from "@/features/admin/settings/types";
import type { PurchaseGiftCardRequest } from "../types";

export const GIFT_SEND_MODE = {
  Now: "now",
  Scheduled: "scheduled",
} as const;
export type GiftSendMode = (typeof GIFT_SEND_MODE)[keyof typeof GIFT_SEND_MODE];

export type GiftCardLimits = Pick<
  CinemaSettingsDto,
  "giftCardDenominations" | "giftCardMinAmount" | "giftCardMaxAmount" | "giftCardValidityMonths"
>;

export const parseAmount = (value: string) => Number(value.replace(/\D/g, ""));

const addMonths = (from: number, months: number) => {
  const date = new Date(from);
  date.setMonth(date.getMonth() + months);
  return date.getTime();
};

export const createGiftCardSchema = (limits: GiftCardLimits) =>
  z
    .object({
      amount: z.string().trim().min(1, "Vui lòng chọn hoặc nhập mệnh giá."),
      recipientName: z
        .string()
        .trim()
        .min(1, "Vui lòng nhập tên người nhận.")
        .max(GIFT_CARD_RECIPIENT_NAME_MAX_LENGTH, `Tên người nhận tối đa ${GIFT_CARD_RECIPIENT_NAME_MAX_LENGTH} ký tự.`),
      recipientEmail: z
        .string()
        .trim()
        .min(1, VALIDATION_MESSAGES.emailRequired)
        .email(VALIDATION_MESSAGES.emailInvalid)
        .max(EMAIL_MAX_LENGTH, `Email tối đa ${EMAIL_MAX_LENGTH} ký tự.`),
      message: z
        .string()
        .max(GIFT_CARD_MESSAGE_MAX_LENGTH, `Lời nhắn tối đa ${GIFT_CARD_MESSAGE_MAX_LENGTH} ký tự.`),
      sendMode: z.enum([GIFT_SEND_MODE.Now, GIFT_SEND_MODE.Scheduled]),
      sendAt: z.string(),
    })
    .superRefine((values, ctx) => {
      const amount = parseAmount(values.amount);
      if (values.amount.trim() && amount < limits.giftCardMinAmount) {
        ctx.addIssue({
          code: "custom",
          path: ["amount"],
          message: `Mệnh giá tối thiểu ${formatCurrency(limits.giftCardMinAmount)}.`,
        });
      } else if (amount > limits.giftCardMaxAmount) {
        ctx.addIssue({
          code: "custom",
          path: ["amount"],
          message: `Mệnh giá tối đa ${formatCurrency(limits.giftCardMaxAmount)}.`,
        });
      }

      if (values.sendMode !== GIFT_SEND_MODE.Scheduled) return;
      if (!values.sendAt) {
        ctx.addIssue({ code: "custom", path: ["sendAt"], message: "Vui lòng chọn thời điểm gửi." });
        return;
      }
      const sendAt = Date.parse(toBusinessIso(values.sendAt));
      const now = getServerNow();
      if (Number.isNaN(sendAt) || sendAt <= now) {
        ctx.addIssue({ code: "custom", path: ["sendAt"], message: "Thời điểm gửi phải ở tương lai." });
      } else if (sendAt >= addMonths(now, limits.giftCardValidityMonths)) {
        ctx.addIssue({
          code: "custom",
          path: ["sendAt"],
          message: `Chỉ hẹn gửi trong vòng ${limits.giftCardValidityMonths} tháng tới.`,
        });
      }
    });

export type GiftCardFormValues = z.infer<ReturnType<typeof createGiftCardSchema>>;

export const getGiftCardDefaults = (limits: GiftCardLimits): GiftCardFormValues => ({
  amount: String(limits.giftCardDenominations[0] ?? limits.giftCardMinAmount),
  recipientName: "",
  recipientEmail: "",
  message: "",
  sendMode: GIFT_SEND_MODE.Now,
  sendAt: "",
});

export const toPurchaseGiftCardRequest = (values: GiftCardFormValues): PurchaseGiftCardRequest => ({
  amount: parseAmount(values.amount),
  recipientName: values.recipientName.trim(),
  recipientEmail: values.recipientEmail.trim(),
  message: values.message.trim() || null,
  sendAt: values.sendMode === GIFT_SEND_MODE.Scheduled ? toBusinessIso(values.sendAt) : null,
});
