import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FormActions } from "@/components/common/form/form-actions";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { formatPoints } from "@/lib/formatters";

import { useAdminAdjustLoyaltyPoints } from "../../hooks/use-admin-loyalty-customers";
import type { LoyaltyCustomerDetailDto } from "../../types";
import {
  ADJUSTMENT_DIRECTION,
  ADJUSTMENT_DIRECTION_OPTIONS,
  ADJUSTMENT_MAX_POINTS,
} from "../../utils/loyalty-admin-labels";
import { adjustPointsFormSchema, type AdjustPointsFormValues } from "./adjust-points-schema";

interface AdjustPointsFormProps {
  customer: LoyaltyCustomerDetailDto;
  onSuccess: () => void;
}

export const AdjustPointsForm = ({ customer, onSuccess }: AdjustPointsFormProps) => {
  const adjust = useAdminAdjustLoyaltyPoints();
  const isPending = adjust.isPending;

  const form = useForm<AdjustPointsFormValues>({
    resolver: zodResolver(adjustPointsFormSchema),
    defaultValues: {
      direction: ADJUSTMENT_DIRECTION.Add,
      points: "",
      reason: "",
    },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: AdjustPointsFormValues) => {
    const amount = Number(values.points);
    adjust.mutate(
      {
        userId: customer.userId,
        body: {
          points: values.direction === ADJUSTMENT_DIRECTION.Subtract ? -amount : amount,
          reason: values.reason,
        },
      },
      { onSuccess: () => onSuccess(), onError: applyServerErrors },
    );
  };

  return (
    <form
      id="loyalty-adjust-points-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <div className="space-y-0.5 bg-muted/50 p-3 text-sm">
        <p className="font-medium text-foreground">{customer.fullName}</p>
        <p className="text-muted-foreground">
          Số dư hiện tại: <span className="tabular-nums">{formatPoints(customer.balance)}</span> điểm
        </p>
      </div>

      <FieldGroup disabled={isPending}>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormSelectField
            name="direction"
            control={form.control}
            label="Thao tác"
            options={[...ADJUSTMENT_DIRECTION_OPTIONS]}
            getOptionValue={(o) => o.value}
            getOptionLabel={(o) => o.label}
          />
          <FormInputField
            name="points"
            control={form.control}
            label="Số điểm"
            type="number"
            min={1}
            max={ADJUSTMENT_MAX_POINTS}
            step={1}
            placeholder="100"
            autoFocus
          />
        </div>

        <FormTextareaField
          name="reason"
          control={form.control}
          label="Lý do điều chỉnh"
          placeholder="Ví dụ: Bù điểm do lỗi tích điểm đơn #1234"
          rows={3}
        />
        <p className="-mt-2 text-xs text-muted-foreground">
          Lý do bắt buộc và được ghi vào nhật ký thao tác cùng người thực hiện.
        </p>
      </FieldGroup>

      <FormErrorAlert error={adjust.error} preferServerMessage />

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Xác nhận điều chỉnh"
        onCancel={onSuccess}
      />
    </form>
  );
};
