import { VALIDATION_MESSAGES } from "@/config/error-messages";
import { z } from "zod";

import { strongPasswordSchema } from "@/lib/validators";

export const adminSetPasswordSchema = z
  .object({
    newPassword: strongPasswordSchema,
    confirmPassword: z.string().min(1, VALIDATION_MESSAGES.confirmPasswordRequired),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: VALIDATION_MESSAGES.passwordMismatch,
    path: ["confirmPassword"],
  });

export type AdminSetPasswordFormValues = z.infer<typeof adminSetPasswordSchema>;
