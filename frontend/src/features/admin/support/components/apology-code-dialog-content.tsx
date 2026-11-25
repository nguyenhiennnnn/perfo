import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, useWatch } from "react-hook-form";
import { z } from "zod";
import { FormActions } from "@/components/common/form/form-actions";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FieldGroup } from "@/components/ui/field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useDialogClose } from "@/stores/dialog-store";
import { PromotionDiscountType } from "@/types/enum";
import {
  APOLOGY_DEFAULT_VALID_DAYS,
  APOLOGY_MAX_AMOUNT,
  APOLOGY_MAX_VALID_DAYS,
} from "../constants";
import { useIssueSupportApologyCode } from "../hooks/use-admin-support";

const TYPE_OPTIONS = [
  { value: PromotionDiscountType.FixedAmount, label: "Số tiền cố định" },
  { value: PromotionDiscountType.Percent, label: "Phần trăm" },
];

const isPositiveInteger = (value: string) => /^\d+$/.test(value) && Number(value) > 0;

const schema = z
  .object({
    discountType: z.string().min(1, "Vui lòng chọn loại giảm."),
    discountValue: z.string().trim().min(1, "Vui lòng nhập mức giảm."),
    maxDiscountAmount: z.string().trim(),
    validDays: z.string().trim(),
  })
  .superRefine((values, ctx) => {
    const isPercent = values.discountType === PromotionDiscountType.Percent;
    const value = Number(values.discountValue);

    if (values.discountValue) {
      if (isPercent) {
        const valid = Number.isFinite(value) && value > 0 && value <= 100 && Math.round(value * 100) / 100 === value;
        if (!valid)
          ctx.addIssue({ code: "custom", path: ["discountValue"], message: "Phần trăm giảm từ 0 đến 100, tối đa 2 chữ số thập phân." });
      } else if (!isPositiveInteger(values.discountValue) || value > APOLOGY_MAX_AMOUNT) {
        ctx.addIssue({ code: "custom", path: ["discountValue"], message: "Số tiền giảm phải là số nguyên dương hợp lệ." });
      }
    }

    if (isPercent && values.maxDiscountAmount) {
      if (!isPositiveInteger(values.maxDiscountAmount) || Number(values.maxDiscountAmount) > APOLOGY_MAX_AMOUNT)
        ctx.addIssue({ code: "custom", path: ["maxDiscountAmount"], message: "Mức giảm tối đa phải là số nguyên dương hợp lệ." });
    }

    if (values.validDays) {
      if (!isPositiveInteger(values.validDays) || Number(values.validDays) > APOLOGY_MAX_VALID_DAYS)
        ctx.addIssue({ code: "custom", path: ["validDays"], message: `Hạn dùng từ 1 đến ${APOLOGY_MAX_VALID_DAYS} ngày.` });
    }
  });

type FormValues = z.infer<typeof schema>;

interface ApologyCodeDialogContentProps {
  requestId: number;
}

export const ApologyCodeDialogContent = ({ requestId }: ApologyCodeDialogContentProps) => {
  const close = useDialogClose();
  const issue = useIssueSupportApologyCode(requestId);
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onTouched",
    defaultValues: {
      discountType: PromotionDiscountType.FixedAmount,
      discountValue: "",
      maxDiscountAmount: "",
      validDays: String(APOLOGY_DEFAULT_VALID_DAYS),
    },
  });
  const applyServerErrors = useServerFieldErrors(form);
  const discountType = useWatch({ control: form.control, name: "discountType" });
  const isPercent = discountType === PromotionDiscountType.Percent;

  const onSubmit = (values: FormValues) =>
    issue.mutate(
      {
        discountType: values.discountType as PromotionDiscountType,
        discountValue: Number(values.discountValue),
        maxDiscountAmount: isPercent && values.maxDiscountAmount ? Number(values.maxDiscountAmount) : null,
        validDays: values.validDays ? Number(values.validDays) : null,
      },
      { onSuccess: close, onError: applyServerErrors },
    );

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5 pb-6">
      <FieldGroup disabled={issue.isPending} className="gap-4">
        <FormSelectField
          name="discountType"
          control={form.control}
          label="Loại giảm"
          options={TYPE_OPTIONS}
          getOptionValue={(option) => option.value}
          getOptionLabel={(option) => option.label}
        />
        <FormInputField
          name="discountValue"
          control={form.control}
          label={isPercent ? "Phần trăm giảm (%)" : "Số tiền giảm (VND)"}
          type="number"
          min={0}
          step={isPercent ? 0.01 : 1}
          autoFocus
        />
        {isPercent && (
          <FormInputField
            name="maxDiscountAmount"
            control={form.control}
            label="Giảm tối đa (VND, không bắt buộc)"
            type="number"
            min={1}
            step={1}
          />
        )}
        <FormInputField
          name="validDays"
          control={form.control}
          label="Hạn dùng (ngày)"
          type="number"
          min={1}
          max={APOLOGY_MAX_VALID_DAYS}
          step={1}
        />
      </FieldGroup>
      <p className="text-sm text-muted-foreground">
        Mã khuyến mãi cá nhân dành riêng cho khách của yêu cầu này, dùng được một lần. Khách nhận mã qua email.
      </p>
      <FormActions
        isPending={issue.isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Cấp mã xin lỗi"
        onCancel={close}
      />
    </form>
  );
};
