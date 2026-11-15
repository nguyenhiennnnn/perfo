import { VALIDATION_MESSAGES } from "@/config/error-messages";
import { z } from "zod";

import { strongPasswordSchema } from "@/lib/validators";

export interface SetPasswordFormValues {
  newPassword: string;
  confirmPassword: string;
}

export interface ChangePasswordFormValues extends SetPasswordFormValues {
  currentPassword: string;
}

export const setPasswordSchema: z.ZodType<SetPasswordFormValues> = z
  .object({
    newPassword: strongPasswordSchema,
    confirmPassword: z.string().min(1, "Vui lòng xác nhận mật khẩu."),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: VALIDATION_MESSAGES.passwordMismatch,
    path: ["confirmPassword"],
  });

export const changePasswordSchema: z.ZodType<ChangePasswordFormValues> = z
  .object({
    currentPassword: z.string().min(1, "Vui lòng nhập mật khẩu hiện tại."),
    newPassword: strongPasswordSchema,
    confirmPassword: z.string().min(1, "Vui lòng xác nhận mật khẩu mới."),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: VALIDATION_MESSAGES.passwordMismatch,
    path: ["confirmPassword"],
  });
