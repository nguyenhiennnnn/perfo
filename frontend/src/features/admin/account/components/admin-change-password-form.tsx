import type { ReactNode } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { PasswordStrengthMeter } from "@/components/common/form/password-strength-meter";
import { useChangePassword } from "@/features/client/account/hooks/use-account";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";

import {
  adminChangePasswordSchema,
  type AdminChangePasswordFormValues,
} from "../schemas/change-password-schema";

interface AdminChangePasswordFormProps {
  submitLabel: string;
  submitIcon?: ReactNode;
  skipSuccessToast?: boolean;
  onSuccess?: () => void;
}

export const AdminChangePasswordForm = ({
  submitLabel,
  submitIcon,
  skipSuccessToast,
  onSuccess,
}: AdminChangePasswordFormProps) => {
  const changePassword = useChangePassword({ skipSuccessToast });

  const form = useForm<AdminChangePasswordFormValues>({
    resolver: zodResolver(adminChangePasswordSchema),
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const newPassword = form.watch("newPassword");

  const onSubmit = (values: AdminChangePasswordFormValues) => {
    changePassword.mutate(
      { currentPassword: values.currentPassword, newPassword: values.newPassword },
      {
        onSuccess: () => {
          form.reset();
          onSuccess?.();
        },
        onError: applyServerErrors,
      },
    );
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
      <FieldGroup disabled={changePassword.isPending} className="gap-4">
        <FormInputField
          name="currentPassword"
          control={form.control}
          label="Mật khẩu hiện tại"
          type="password"
          autoComplete="current-password"
        />
        <div>
          <FormInputField
            name="newPassword"
            control={form.control}
            label="Mật khẩu mới"
            type="password"
            autoComplete="new-password"
          />
          <PasswordStrengthMeter password={newPassword} />
        </div>
        <FormInputField
          name="confirmPassword"
          control={form.control}
          label="Xác nhận mật khẩu mới"
          type="password"
          autoComplete="new-password"
        />
      </FieldGroup>

      <SubmitButton
        isPending={changePassword.isPending}
        label={submitLabel}
        icon={submitIcon}
        className="w-full"
      />
    </form>
  );
};
