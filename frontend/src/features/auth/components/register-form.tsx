import { VALIDATION_MESSAGES } from "@/config/error-messages";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Link } from "react-router";
import { CheckCircle2 } from "lucide-react";
import { z } from "zod";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Field, FieldContent, FieldDescription, FieldGroup, FieldLabel } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { SubmitButton } from "@/components/common/form/submit-button";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useRegister } from "../hooks/use-auth";
import { useRevalidateWhenTouched } from "@/hooks/use-revalidate-when-touched";
import { strongPasswordSchema } from "@/lib/validators";
import type { RegisterRequest } from "../types";
import { PasswordStrengthMeter } from "@/components/common/form/password-strength-meter";
import { ROUTES } from "@/config/routes";

import { PERSON_NAME_PLACEHOLDER, PASSWORD_PLACEHOLDER } from "@/config/default-constants";
const registerSchema = z
  .object({
    fullName: z.string().min(2, "Họ tên phải có ít nhất 2 ký tự."),
    email: z
      .string()
      .min(1, VALIDATION_MESSAGES.emailRequired)
      .email(VALIDATION_MESSAGES.emailInvalid),
    phone: z
      .string()
      .optional()
      .refine(
        (v) => !v || /^0\d{9,10}$/.test(v),
        VALIDATION_MESSAGES.phoneInvalid,
      ),
    password: strongPasswordSchema,
    confirmPassword: z.string().min(1, VALIDATION_MESSAGES.confirmPasswordRequired),
    marketingOptIn: z.boolean(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: VALIDATION_MESSAGES.passwordMismatch,
    path: ["confirmPassword"],
  });

type RegisterFormValues = z.infer<typeof registerSchema>;

export const RegisterForm = () => {
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null);
  const { mutate: registerUser, isPending, error } = useRegister();

  const form = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    mode: "onTouched",
    defaultValues: {
      fullName: "",
      email: "",
      phone: "",
      password: "",
      confirmPassword: "",
      marketingOptIn: false,
    },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (data: RegisterFormValues) => {
    const payload: RegisterRequest = {
      fullName: data.fullName,
      email: data.email,
      password: data.password,
      phone: data.phone || null,
      marketingOptIn: data.marketingOptIn,
    };
    registerUser(payload, {
      onSuccess: () => setSubmittedEmail(data.email),
      onError: applyServerErrors,
    });
  };

  const password = form.watch("password");
  useRevalidateWhenTouched(
    password,
    () => form.getFieldState("confirmPassword").isTouched,
    () => void form.trigger("confirmPassword"),
  );

  if (submittedEmail) {
    return (
      <div className="flex flex-col items-center gap-3 text-center">
        <CheckCircle2 className="size-10 text-success" />
        <h2 className="t-h2 text-foreground">
          Đăng ký thành công
        </h2>
        <p className="text-sm text-muted-foreground">
          Vui lòng kiểm tra hộp thư{" "}
          <span className="font-medium text-foreground">{submittedEmail}</span>{" "}
          để xác thực tài khoản.
        </p>
        <Button
          variant="outline"
          render={<Link to={ROUTES.auth.login} />}
          nativeButton={false}
        >
          Về trang đăng nhập
        </Button>
      </div>
    );
  }

  return (
    <form
      onSubmit={form.handleSubmit(onSubmit)}
      className="flex flex-col gap-4"
    >
      <FieldGroup disabled={isPending} className="gap-4">
        <FormInputField
          name="fullName"
          control={form.control}
          label="Họ và tên"
          placeholder={PERSON_NAME_PLACEHOLDER}
        />

        <FormInputField
          name="email"
          control={form.control}
          label="Email"
          type="email"
          autoComplete="email"
          placeholder="you@example.com"
        />

        <FormInputField
          name="phone"
          control={form.control}
          label="Số điện thoại (không bắt buộc)"
          placeholder="0901234567"
        />

        <div className="flex flex-col gap-2">
          <FormInputField
            name="password"
            control={form.control}
            label="Mật khẩu"
            type="password"
            autoComplete="new-password"
            placeholder={PASSWORD_PLACEHOLDER}
          />
          <PasswordStrengthMeter password={password} />
        </div>

        <FormInputField
          name="confirmPassword"
          control={form.control}
          label="Xác nhận mật khẩu"
          type="password"
          autoComplete="new-password"
          placeholder={PASSWORD_PLACEHOLDER}
        />

        <Controller
          name="marketingOptIn"
          control={form.control}
          render={({ field }) => (
            <Field orientation="horizontal" className="items-start">
              <Checkbox
                id="register-marketing-opt-in"
                checked={field.value}
                onCheckedChange={field.onChange}
              />
              <FieldContent>
                <FieldLabel htmlFor="register-marketing-opt-in">
                  Nhận email và thông báo tiếp thị
                </FieldLabel>
                <FieldDescription>
                  Ưu đãi, khuyến mãi và tin phim mới. Bạn có thể tắt bất cứ lúc nào.
                </FieldDescription>
              </FieldContent>
            </Field>
          )}
        />
      </FieldGroup>

      <FormErrorAlert error={error} />

      <SubmitButton isPending={isPending} label="Đăng ký" className="w-full" />

      <p className="text-center text-sm text-muted-foreground">
        Đã có tài khoản?{" "}
        <Link
          to={ROUTES.auth.login}
          className="font-medium text-primary-text hover:underline"
        >
          Đăng nhập
        </Link>
      </p>
    </form>
  );
};