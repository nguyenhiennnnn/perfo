import { z } from "zod";

import { formatPoints } from "@/lib/formatters";

import {
  ADJUSTMENT_DIRECTION,
  ADJUSTMENT_MAX_POINTS,
  ADJUSTMENT_REASON_MAX_LENGTH,
} from "../../utils/loyalty-admin-labels";

const INTEGER_PATTERN = /^\d+$/;
const CONTROL_CHARS_PATTERN = /[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/;

export const adjustPointsFormSchema = z.object({
  direction: z.enum([ADJUSTMENT_DIRECTION.Add, ADJUSTMENT_DIRECTION.Subtract]),
  points: z
    .string()
    .trim()
    .refine((value) => value !== "", "Vui lòng nhập số điểm.")
    .refine((value) => value === "" || INTEGER_PATTERN.test(value), "Số điểm phải là số nguyên dương.")
    .refine(
      (value) => !INTEGER_PATTERN.test(value) || Number(value) >= 1,
      "Số điểm phải lớn hơn 0.",
    )
    .refine(
      (value) => !INTEGER_PATTERN.test(value) || Number(value) <= ADJUSTMENT_MAX_POINTS,
      `Số điểm tối đa mỗi lần là ${formatPoints(ADJUSTMENT_MAX_POINTS)}.`,
    ),
  reason: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập lý do điều chỉnh.")
    .max(ADJUSTMENT_REASON_MAX_LENGTH, `Lý do tối đa ${ADJUSTMENT_REASON_MAX_LENGTH} ký tự.`)
    .refine((value) => !CONTROL_CHARS_PATTERN.test(value), "Lý do chứa ký tự không hợp lệ."),
});

export type AdjustPointsFormValues = z.infer<typeof adjustPointsFormSchema>;
