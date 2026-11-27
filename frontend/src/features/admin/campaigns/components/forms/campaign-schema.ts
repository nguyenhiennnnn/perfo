import { z } from "zod";

import { getBusinessNow, toBusinessIso, toWallDate } from "@/lib/business-time";
import { toDateTimeInputValue } from "@/lib/formatters";
import { CampaignChannel, PromotionDiscountType } from "@/types/enum";

import type { CampaignDto, UpsertCampaignRequest } from "../../types";
import {
  CAMPAIGN_BODY_MAX_LENGTH,
  CAMPAIGN_NAME_MAX_LENGTH,
  CAMPAIGN_PROMO_MAX_AMOUNT,
  CAMPAIGN_PROMO_MAX_VALIDITY_DAYS,
  CAMPAIGN_SUBJECT_MAX_LENGTH,
} from "../../utils/campaign-labels";

const PERCENT_PATTERN = /^\d+(\.\d{1,2})?$/;
const INTEGER_PATTERN = /^\d+$/;

const isBlank = (value: string) => value.trim() === "";

const isAmount = (value: string) =>
  INTEGER_PATTERN.test(value.trim()) &&
  Number(value) >= 1 &&
  Number(value) <= CAMPAIGN_PROMO_MAX_AMOUNT;

export const campaignFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tên chiến dịch.")
      .max(CAMPAIGN_NAME_MAX_LENGTH, `Tên chiến dịch tối đa ${CAMPAIGN_NAME_MAX_LENGTH} ký tự.`),
    segmentId: z.string({
      required_error: "Vui lòng chọn phân khúc khách hàng.",
      invalid_type_error: "Vui lòng chọn phân khúc khách hàng.",
    }).min(1, "Vui lòng chọn phân khúc khách hàng."),
    channel: z.enum([CampaignChannel.Email, CampaignChannel.InApp]),
    subject: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tiêu đề.")
      .max(CAMPAIGN_SUBJECT_MAX_LENGTH, `Tiêu đề tối đa ${CAMPAIGN_SUBJECT_MAX_LENGTH} ký tự.`),
    body: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập nội dung.")
      .max(CAMPAIGN_BODY_MAX_LENGTH, `Nội dung tối đa ${CAMPAIGN_BODY_MAX_LENGTH} ký tự.`),
    hasPromo: z.boolean(),
    promo: z.object({
      discountType: z.enum([PromotionDiscountType.Percent, PromotionDiscountType.FixedAmount]),
      discountValue: z.string(),
      maxDiscountAmount: z.string(),
      validityDays: z.string(),
    }),
    scheduledAt: z.string(),
  })
  .superRefine((values, ctx) => {
    const addIssue = (path: (string | number)[], message: string) =>
      ctx.addIssue({ code: "custom", path, message });

    if (!isBlank(values.scheduledAt)) {
      const scheduled = toWallDate(values.scheduledAt);
      if (!scheduled) {
        addIssue(["scheduledAt"], "Thời điểm hẹn gửi không hợp lệ.");
      } else if (scheduled.getTime() <= getBusinessNow().getTime()) {
        addIssue(["scheduledAt"], "Thời điểm hẹn gửi phải ở tương lai.");
      }
    }

    if (!values.hasPromo) return;
    const { promo } = values;
    const discountValue = promo.discountValue.trim();

    if (promo.discountType === PromotionDiscountType.Percent) {
      const valid =
        PERCENT_PATTERN.test(discountValue) &&
        Number(discountValue) > 0 &&
        Number(discountValue) <= 100;
      if (!valid) {
        addIssue(
          ["promo", "discountValue"],
          "Phần trăm giảm phải lớn hơn 0 và tối đa 100, tối đa 2 chữ số thập phân.",
        );
      }
      if (!isBlank(promo.maxDiscountAmount) && !isAmount(promo.maxDiscountAmount)) {
        addIssue(["promo", "maxDiscountAmount"], "Mức giảm tối đa phải là số tiền nguyên lớn hơn 0.");
      }
    } else if (!isAmount(discountValue)) {
      addIssue(["promo", "discountValue"], "Số tiền giảm phải là số nguyên lớn hơn 0.");
    }

    const validityDays = promo.validityDays.trim();
    if (
      !INTEGER_PATTERN.test(validityDays) ||
      Number(validityDays) < 1 ||
      Number(validityDays) > CAMPAIGN_PROMO_MAX_VALIDITY_DAYS
    ) {
      addIssue(
        ["promo", "validityDays"],
        `Số ngày hiệu lực của mã phải từ 1 đến ${CAMPAIGN_PROMO_MAX_VALIDITY_DAYS}.`,
      );
    }
  });

export type CampaignFormValues = z.infer<typeof campaignFormSchema>;

export const getCampaignFormDefaults = (campaign?: CampaignDto): CampaignFormValues => ({
  name: campaign?.name ?? "",
  segmentId: campaign ? String(campaign.segmentId) : "",
  channel: campaign?.channel ?? CampaignChannel.Email,
  subject: campaign?.subject ?? "",
  body: campaign?.body ?? "",
  hasPromo: campaign?.promo != null,
  promo: {
    discountType: campaign?.promo?.discountType ?? PromotionDiscountType.Percent,
    discountValue: campaign?.promo ? String(campaign.promo.discountValue) : "",
    maxDiscountAmount:
      campaign?.promo?.maxDiscountAmount != null ? String(campaign.promo.maxDiscountAmount) : "",
    validityDays: campaign?.promo ? String(campaign.promo.validityDays) : "30",
  },
  scheduledAt: toDateTimeInputValue(campaign?.scheduledAt) ?? "",
});

export const toCampaignRequest = (values: CampaignFormValues): UpsertCampaignRequest => {
  const isPercent = values.promo.discountType === PromotionDiscountType.Percent;
  const maxDiscount = values.promo.maxDiscountAmount.trim();

  return {
    name: values.name,
    segmentId: Number(values.segmentId),
    channel: values.channel,
    subject: values.subject,
    body: values.body,
    promo: values.hasPromo
      ? {
          discountType: values.promo.discountType,
          discountValue: Number(values.promo.discountValue),
          maxDiscountAmount: isPercent && maxDiscount !== "" ? Number(maxDiscount) : null,
          validityDays: Number(values.promo.validityDays),
        }
      : null,
    scheduledAt: values.scheduledAt ? toBusinessIso(values.scheduledAt) : null,
  };
};
