import { useForm, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormDatePickerField } from "@/components/common/form/form-date-picker-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FormActions } from "@/components/common/form/form-actions";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { getBusinessToday, getBusinessTodayDate } from "@/lib/business-time";

import { useAdminCreateStocktake } from "../../hooks/use-admin-stock-sheets";
import {
  EMPTY_INVENTORY_LINE,
  stocktakeFormSchema,
  type StocktakeFormValues,
} from "../../utils/inventory-schema";
import { InventoryLinesField } from "./inventory-lines-field";

interface StocktakeFormProps {
  onSuccess: () => void;
}

export const StocktakeForm = ({ onSuccess }: StocktakeFormProps) => {
  const createStocktake = useAdminCreateStocktake();

  const form = useForm<StocktakeFormValues>({
    resolver: zodResolver(stocktakeFormSchema),
    defaultValues: {
      stocktakeDate: getBusinessToday(),
      note: "",
      lines: [EMPTY_INVENTORY_LINE],
    },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: StocktakeFormValues) => {
    createStocktake.mutate(
      {
        stocktakeDate: values.stocktakeDate,
        note: values.note || null,
        lines: values.lines.map((line) => ({
          inventoryItemId: Number(line.inventoryItemId),
          countedQuantity: Number(line.quantity),
        })),
      },
      { onSuccess: () => onSuccess(), onError: applyServerErrors },
    );
  };

  return (
    <FormProvider {...form}>
      <form id="stocktake-form" onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FieldGroup disabled={createStocktake.isPending}>
          <FormDatePickerField
            name="stocktakeDate"
            control={form.control}
            label="Ngày kiểm kê"
            clearable={false}
            calendarDisabled={{ after: getBusinessTodayDate() }}
          />
          <FormTextareaField name="note" control={form.control} label="Ghi chú" rows={2} />
          <InventoryLinesField mode="stocktake" />
        </FieldGroup>

        <FormActions
          isPending={createStocktake.isPending}
          isDirty={form.formState.isDirty}
          submitLabel="Lưu phiếu kiểm kê"
          onCancel={onSuccess}
        />
      </form>
    </FormProvider>
  );
};
