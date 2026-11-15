import { z } from "zod";

import { strongPasswordSchema } from "@/lib/validators";

export const adminSetPasswordSchema = z
  .object({
    newPassword: strongPasswordSchema,
    confirmPassword: z.string().min(1, "Vui lòng xác nhận mật khẩu."),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Mật khẩu xác nhận không khớp.",
    path: ["confirmPassword"],
  });

export type AdminSetPasswordFormValues = z.infer<typeof adminSetPasswordSchema>;
