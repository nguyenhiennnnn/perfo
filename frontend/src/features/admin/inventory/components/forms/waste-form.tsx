import { useForm, FormProvider, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormDatePickerField } from "@/components/common/form/form-date-picker-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FormActions } from "@/components/common/form/form-actions";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { getBusinessToday, getBusinessTodayDate } from "@/lib/business-time";

import { useAdminCreateWasteSheet } from "../../hooks/use-admin-stock-sheets";
import { WasteReason } from "../../types";
import { WASTE_REASON_OPTIONS } from "../../utils/inventory-labels";
import {
  EMPTY_INVENTORY_LINE,
  wasteFormSchema,
  type WasteFormValues,
} from "../../utils/inventory-schema";
import { InventoryLinesField } from "./inventory-lines-field";
import { OptionSelectField } from "./option-select-field";

interface WasteFormProps {
  onSuccess: () => void;
}

export const WasteForm = ({ onSuccess }: WasteFormProps) => {
  const createWaste = useAdminCreateWasteSheet();

  const form = useForm<WasteFormValues>({
    resolver: zodResolver(wasteFormSchema),
    defaultValues: {
      wasteDate: getBusinessToday(),
      reason: WasteReason.Expired,
      note: "",
      lines: [EMPTY_INVENTORY_LINE],
    },
  });

  const applyServerErrors = useServerFieldErrors(form);
  const reason = useWatch({ control: form.control, name: "reason" });

  const onSubmit = (values: WasteFormValues) => {
    createWaste.mutate(
      {
        wasteDate: values.wasteDate,
        reason: values.reason,
        note: values.note || null,
        lines: values.lines.map((line) => ({
          inventoryItemId: Number(line.inventoryItemId),
          quantity: Number(line.quantity),
        })),
      },
      { onSuccess: () => onSuccess(), onError: applyServerErrors },
    );
  };

  return (
    <FormProvider {...form}>
      <form id="waste-sheet-form" onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FieldGroup disabled={createWaste.isPending}>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <FormDatePickerField
              name="wasteDate"
              control={form.control}
              label="Ngày hủy hàng"
              clearable={false}
              calendarDisabled={{ after: getBusinessTodayDate() }}
            />
            <OptionSelectField
              name="reason"
              control={form.control}
              label="Lý do"
              options={WASTE_REASON_OPTIONS}
            />
          </div>
          <FormTextareaField
            name="note"
            control={form.control}
            label={reason === WasteReason.Other ? "Ghi chú (bắt buộc)" : "Ghi chú"}
            rows={2}
          />
          <InventoryLinesField mode="waste" />
        </FieldGroup>

        <FormActions
          isPending={createWaste.isPending}
          isDirty={form.formState.isDirty}
          submitLabel="Lưu phiếu hao hụt"
          onCancel={onSuccess}
        />
      </form>
    </FormProvider>
  );
};
