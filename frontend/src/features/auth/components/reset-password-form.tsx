import { useForm } from "react-hook-form";
import { useNavigate, useSearchParams } from "react-router";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { SubmitButton } from "@/components/common/form/submit-button";
import { PasswordStrengthMeter } from "@/components/common/form/password-strength-meter";
import { ROUTES } from "@/config/routes";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useRevalidateWhenTouched } from "@/hooks/use-revalidate-when-touched";

import { useResetPassword } from "../hooks/use-auth";
import { setPasswordSchema, type SetPasswordFormValues } from "../schemas/password-schemas";
import { InvalidLinkNotice } from "./invalid-link-notice";

import { PASSWORD_PLACEHOLDER } from "@/config/default-constants";
export const ResetPasswordForm = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";

  const { mutate: resetPassword, isPending, error } = useResetPassword();

  const form = useForm<SetPasswordFormValues>({
    resolver: zodResolver(setPasswordSchema),
    mode: "onTouched",
    defaultValues: { newPassword: "", confirmPassword: "" },
  });

  const applyServerErrors = useServerFieldErrors(form);
  const newPassword = form.watch("newPassword");
  useRevalidateWhenTouched(
    newPassword,
    () => form.getFieldState("confirmPassword").isTouched,
    () => void form.trigger("confirmPassword"),
  );

  if (!token) {
    return (
      <InvalidLinkNotice
        title="Liên kết không hợp lệ"
        description="Thiếu mã đặt lại mật khẩu trong đường dẫn."
        actionLabel="Yêu cầu liên kết mới"
        actionTo={ROUTES.auth.forgotPassword}
      />
    );
  }

  const onSubmit = (data: SetPasswordFormValues) => {
    resetPassword(
      { token, newPassword: data.newPassword },
      {
        onSuccess: () => navigate(ROUTES.auth.login, { replace: true }),
        onError: applyServerErrors,
      },
    );
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
      <FieldGroup disabled={isPending} className="gap-4">
        <FormInputField
          name="newPassword"
          control={form.control}
          label="Mật khẩu mới"
          type="password"
          autoComplete="new-password"
          placeholder={PASSWORD_PLACEHOLDER}
        />

        <FormInputField
          name="confirmPassword"
          control={form.control}
          label="Xác nhận mật khẩu mới"
          type="password"
          autoComplete="new-password"
          placeholder={PASSWORD_PLACEHOLDER}
        />

        <PasswordStrengthMeter password={newPassword} />
      </FieldGroup>

      <FormErrorAlert error={error} />

      <SubmitButton isPending={isPending} label="Đặt lại mật khẩu" className="w-full" />
    </form>
  );
};