import { z } from "zod";

import { COMBO_NAME_MAX_LENGTH } from "@/config/default-constants";

import {
  RULE_MAX_AMOUNT,
  RULE_MAX_HOURS_BEFORE_START,
  RULE_MAX_MULTIPLIER,
  RULE_MAX_PRIORITY,
  RULE_MAX_TIERS,
  RULE_MIN_MULTIPLIER,
} from "../../utils/pricing-labels";

const MULTIPLIER_MESSAGE = `Hệ số phải từ ${RULE_MIN_MULTIPLIER} đến ${RULE_MAX_MULTIPLIER}, khác 1 và tối đa 4 chữ số thập phân.`;
const AMOUNT_MESSAGE = "Số tiền phải khác 0 và có giá trị tuyệt đối tối đa 10.000.000.";
const MULTIPLIER_DECIMALS = 10_000;

const tierSchema = z.object({
  fromPercent: z.string(),
  toPercent: z.string(),
  maxHoursBeforeStart: z.string(),
  value: z.string(),
});

export const pricingRuleFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tên quy tắc.")
      .max(COMBO_NAME_MAX_LENGTH, `Tên quy tắc tối đa ${COMBO_NAME_MAX_LENGTH} ký tự.`),
    ruleType: z.enum(["TimeOfDay", "WeekDay", "SpecialDay", "Occupancy"]),
    adjustmentMode: z.enum(["Multiplier", "Amount"]),
    adjustmentValue: z.string(),
    priority: z.coerce
      .number({ message: "Độ ưu tiên phải là số." })
      .int("Độ ưu tiên phải là số nguyên.")
      .min(0, "Độ ưu tiên không được âm.")
      .max(RULE_MAX_PRIORITY, `Độ ưu tiên tối đa ${RULE_MAX_PRIORITY}.`),
    validFrom: z.string(),
    validTo: z.string(),
    startTime: z.string(),
    endTime: z.string(),
    daysOfWeek: z.array(z.number()),
    specialDayIds: z.array(z.number()),
    tiers: z.array(tierSchema),
    roomIds: z.array(z.number()),
    screenTypeIds: z.array(z.number()),
    movieIds: z.array(z.number()),
    isActive: z.boolean(),
  })
  .superRefine((values, ctx) => {
    const mode = values.adjustmentMode;

    const isValidValue = (raw: string) => {
      if (raw.trim() === "") return false;
      const n = Number(raw);
      if (!Number.isFinite(n)) return false;
      if (mode === "Multiplier") {
        return (
          n >= RULE_MIN_MULTIPLIER &&
          n <= RULE_MAX_MULTIPLIER &&
          n !== 1 &&
          Math.round(n * MULTIPLIER_DECIMALS) / MULTIPLIER_DECIMALS === n
        );
      }
      return n !== 0 && Math.abs(n) <= RULE_MAX_AMOUNT;
    };
    const valueMessage = mode === "Multiplier" ? MULTIPLIER_MESSAGE : AMOUNT_MESSAGE;

    if (values.validFrom && values.validTo && values.validTo < values.validFrom) {
      ctx.addIssue({
        code: "custom",
        path: ["validTo"],
        message: "Ngày kết thúc hiệu lực không được trước ngày bắt đầu.",
      });
    }

    if (values.ruleType !== "Occupancy" && !isValidValue(values.adjustmentValue)) {
      ctx.addIssue({ code: "custom", path: ["adjustmentValue"], message: valueMessage });
    }

    if (values.ruleType === "TimeOfDay") {
      if (!values.startTime || !values.endTime) {
        ctx.addIssue({
          code: "custom",
          path: [values.startTime ? "endTime" : "startTime"],
          message: "Cần nhập cả giờ bắt đầu và giờ kết thúc.",
        });
      } else if (values.startTime >= values.endTime) {
        ctx.addIssue({
          code: "custom",
          path: ["endTime"],
          message: "Giờ kết thúc phải sau giờ bắt đầu.",
        });
      }
    }

    if (values.ruleType === "WeekDay" && values.daysOfWeek.length === 0) {
      ctx.addIssue({
        code: "custom",
        path: ["daysOfWeek"],
        message: "Chọn ít nhất một thứ trong tuần.",
      });
    }

    if (values.ruleType !== "Occupancy") return;

    if (values.tiers.length === 0) {
      ctx.addIssue({ code: "custom", path: ["tiers"], message: "Thêm ít nhất một bậc lấp đầy." });
      return;
    }
    if (values.tiers.length > RULE_MAX_TIERS) {
      ctx.addIssue({
        code: "custom",
        path: ["tiers"],
        message: `Tối đa ${RULE_MAX_TIERS} bậc.`,
      });
      return;
    }

    let tiersValid = true;
    values.tiers.forEach((tier, index) => {
      const from = Number(tier.fromPercent);
      const hasFrom = tier.fromPercent.trim() !== "" && Number.isFinite(from);
      if (!hasFrom || from < 0 || from > 100) {
        tiersValid = false;
        ctx.addIssue({
          code: "custom",
          path: ["tiers", index, "fromPercent"],
          message: "Từ 0 đến 100.",
        });
      }

      if (tier.toPercent.trim() !== "") {
        const to = Number(tier.toPercent);
        if (!Number.isFinite(to) || to > 100 || (hasFrom && to <= from)) {
          tiersValid = false;
          ctx.addIssue({
            code: "custom",
            path: ["tiers", index, "toPercent"],
            message: "Phải lớn hơn mức bắt đầu và tối đa 100.",
          });
        }
      }

      if (tier.maxHoursBeforeStart.trim() !== "") {
        const hours = Number(tier.maxHoursBeforeStart);
        if (
          !Number.isInteger(hours) ||
          hours < 1 ||
          hours > RULE_MAX_HOURS_BEFORE_START
        ) {
          tiersValid = false;
          ctx.addIssue({
            code: "custom",
            path: ["tiers", index, "maxHoursBeforeStart"],
            message: `Từ 1 đến ${RULE_MAX_HOURS_BEFORE_START} giờ.`,
          });
        }
      }

      if (!isValidValue(tier.value)) {
        tiersValid = false;
        ctx.addIssue({
          code: "custom",
          path: ["tiers", index, "value"],
          message: valueMessage,
        });
      }
    });

    if (!tiersValid) return;

    const ordered = [...values.tiers].sort((a, b) => Number(a.fromPercent) - Number(b.fromPercent));
    for (let i = 0; i < ordered.length - 1; i += 1) {
      const upper = ordered[i].toPercent.trim() === "" ? null : Number(ordered[i].toPercent);
      if (upper === null || upper > Number(ordered[i + 1].fromPercent)) {
        ctx.addIssue({
          code: "custom",
          path: ["tiers"],
          message:
            "Các bậc không được chồng lấn; chỉ bậc cuối cùng được để trống mức kết thúc.",
        });
        return;
      }
    }
  });

export type PricingRuleFormValues = z.infer<typeof pricingRuleFormSchema>;
