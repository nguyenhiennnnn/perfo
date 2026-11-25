import { z } from "zod";
import { SupportRequestType } from "@/types/enum";
import { SUPPORT_MESSAGE_MAX_LENGTH, SUPPORT_SUBJECT_MAX_LENGTH } from "../constants";
import type { CreateSupportRequestRequest, ReplySupportRequestRequest } from "../types";

const SUPPORT_TYPE_VALUES = new Set<string>(Object.values(SupportRequestType));

const contentSchema = z
  .string()
  .trim()
  .min(1, "Vui lòng nhập nội dung.")
  .max(SUPPORT_MESSAGE_MAX_LENGTH, `Nội dung tối đa ${SUPPORT_MESSAGE_MAX_LENGTH} ký tự.`);

export const createSupportRequestSchema = z.object({
  type: z
    .string()
    .refine((value) => SUPPORT_TYPE_VALUES.has(value), "Vui lòng chọn loại yêu cầu."),
  subject: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập tiêu đề.")
    .max(SUPPORT_SUBJECT_MAX_LENGTH, `Tiêu đề tối đa ${SUPPORT_SUBJECT_MAX_LENGTH} ký tự.`),
  content: contentSchema,
});

export type CreateSupportRequestFormValues = z.infer<typeof createSupportRequestSchema>;

export const EMPTY_SUPPORT_REQUEST: CreateSupportRequestFormValues = {
  type: "",
  subject: "",
  content: "",
};

export const toCreateSupportRequest = (
  orderId: number,
  values: CreateSupportRequestFormValues,
): CreateSupportRequestRequest => ({
  orderId,
  type: values.type as SupportRequestType,
  subject: values.subject.trim(),
  content: values.content.trim(),
});

export const replySupportRequestSchema = z.object({ content: contentSchema });

export type ReplySupportRequestFormValues = z.infer<typeof replySupportRequestSchema>;

export const toReplySupportRequest = (
  values: ReplySupportRequestFormValues,
): ReplySupportRequestRequest => ({ content: values.content.trim() });
