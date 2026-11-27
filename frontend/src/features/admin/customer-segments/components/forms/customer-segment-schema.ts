import { z } from "zod";

import {
  SEGMENT_DESCRIPTION_MAX_LENGTH,
  SEGMENT_NAME_MAX_LENGTH,
  validateDraft,
  type DraftGroup,
} from "../../utils/segment-condition-fields";

export const customerSegmentFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tên phân khúc.")
      .max(SEGMENT_NAME_MAX_LENGTH, `Tên phân khúc tối đa ${SEGMENT_NAME_MAX_LENGTH} ký tự.`),
    description: z
      .string()
      .trim()
      .max(SEGMENT_DESCRIPTION_MAX_LENGTH, `Mô tả tối đa ${SEGMENT_DESCRIPTION_MAX_LENGTH} ký tự.`),
    isActive: z.boolean(),
    conditions: z.custom<DraftGroup>(),
  })
  .superRefine((values, ctx) => {
    const issue = validateDraft(values.conditions);
    if (issue) ctx.addIssue({ code: "custom", path: ["conditions"], message: issue });
  });

export type CustomerSegmentFormValues = z.infer<typeof customerSegmentFormSchema>;
