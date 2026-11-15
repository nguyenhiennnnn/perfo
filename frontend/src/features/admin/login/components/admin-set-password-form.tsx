import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { SubmitButton } from "@/components/common/form/submit-button";
import { PasswordStrengthMeter } from "@/components/common/form/password-strength-meter";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useResetPassword } from "@/features/auth/hooks/use-auth";

import {
  adminSetPasswordSchema,
  type AdminSetPasswordFormValues,
} from "../schemas/set-password-schema";

interface AdminSetPasswordFormProps {
  token: string;
  onSuccess: () => void;
  onError?: (error: unknown) => void;
}

export const AdminSetPasswordForm = ({
  token,
  onSuccess,
  onError,
}: AdminSetPasswordFormProps) => {
  const {
    mutate: resetPassword,
    isPending,
    error,
  } = useResetPassword({ skipSuccessToast: true });

  const form = useForm<AdminSetPasswordFormValues>({
    resolver: zodResolver(adminSetPasswordSchema),
    defaultValues: { newPassword: "", confirmPassword: "" },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (data: AdminSetPasswordFormValues) => {
    resetPassword(
      { token, newPassword: data.newPassword },
      {
        onSuccess: () => onSuccess(),
        onError: (err) => {
          applyServerErrors(err);
          onError?.(err);
        },
      },
    );
  };

  const newPassword = form.watch("newPassword");

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
      <FieldGroup disabled={isPending} className="gap-4">
        <FormInputField
          name="newPassword"
          control={form.control}
          label="Mật khẩu"
          type="password"
          autoComplete="new-password"
          placeholder="••••••••"
        />

        <FormInputField
          name="confirmPassword"
          control={form.control}
          label="Xác nhận mật khẩu"
          type="password"
          autoComplete="new-password"
          placeholder="••••••••"
        />

        <PasswordStrengthMeter password={newPassword} />
      </FieldGroup>

      <FormErrorAlert error={error} />

      <SubmitButton isPending={isPending} label="Đặt mật khẩu" className="w-full" />
    </form>
  );
};
