import { useForm, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormDatePickerField } from "@/components/common/form/form-date-picker-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FormActions } from "@/components/common/form/form-actions";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { getBusinessToday, getBusinessTodayDate } from "@/lib/business-time";

import { useAdminCreateStockReceipt } from "../../hooks/use-admin-stock-sheets";
import {
  EMPTY_INVENTORY_LINE,
  stockReceiptFormSchema,
  type StockReceiptFormValues,
} from "../../utils/inventory-schema";
import { InventoryLinesField } from "./inventory-lines-field";

interface StockReceiptFormProps {
  onSuccess: () => void;
}

export const StockReceiptForm = ({ onSuccess }: StockReceiptFormProps) => {
  const createReceipt = useAdminCreateStockReceipt();

  const form = useForm<StockReceiptFormValues>({
    resolver: zodResolver(stockReceiptFormSchema),
    defaultValues: {
      supplierName: "",
      receivedDate: getBusinessToday(),
      note: "",
      lines: [EMPTY_INVENTORY_LINE],
    },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: StockReceiptFormValues) => {
    createReceipt.mutate(
      {
        supplierName: values.supplierName,
        receivedDate: values.receivedDate,
        note: values.note || null,
        lines: values.lines.map((line) => ({
          inventoryItemId: Number(line.inventoryItemId),
          quantity: Number(line.quantity),
          unitCost: Number(line.unitCost),
        })),
      },
      { onSuccess: () => onSuccess(), onError: applyServerErrors },
    );
  };

  return (
    <FormProvider {...form}>
      <form id="stock-receipt-form" onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FieldGroup disabled={createReceipt.isPending}>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <FormInputField
              name="supplierName"
              control={form.control}
              label="Nhà cung cấp"
              autoComplete="off"
              autoFocus
            />
            <FormDatePickerField
              name="receivedDate"
              control={form.control}
              label="Ngày nhập"
              clearable={false}
              calendarDisabled={{ after: getBusinessTodayDate() }}
            />
          </div>
          <FormTextareaField name="note" control={form.control} label="Ghi chú" rows={2} />
          <InventoryLinesField mode="receipt" />
        </FieldGroup>

        <FormActions
          isPending={createReceipt.isPending}
          isDirty={form.formState.isDirty}
          submitLabel="Lưu phiếu nhập"
          onCancel={onSuccess}
        />
      </form>
    </FormProvider>
  );
};
