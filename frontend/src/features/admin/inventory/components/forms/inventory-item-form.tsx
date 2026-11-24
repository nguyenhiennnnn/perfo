import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormActions } from "@/components/common/form/form-actions";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { FORM_SAVE_CHANGES_LABEL } from "@/config/default-constants";

import {
  useAdminCreateInventoryItem,
  useAdminUpdateInventoryItem,
} from "../../hooks/use-admin-inventory";
import type { InventoryItemDto } from "../../types";
import { INVENTORY_QUANTITY_STEP } from "../../utils/inventory-labels";
import {
  inventoryItemFormSchema,
  type InventoryItemFormValues,
} from "../../utils/inventory-schema";

interface InventoryItemFormProps {
  item?: InventoryItemDto;
  onSuccess: () => void;
}

export const InventoryItemForm = ({ item, onSuccess }: InventoryItemFormProps) => {
  const isEdit = Boolean(item);
  const createItem = useAdminCreateInventoryItem();
  const updateItem = useAdminUpdateInventoryItem();
  const isPending = createItem.isPending || updateItem.isPending;

  const form = useForm<InventoryItemFormValues>({
    resolver: zodResolver(inventoryItemFormSchema),
    defaultValues: {
      code: item?.code ?? "",
      name: item?.name ?? "",
      unit: item?.unit ?? "",
      lowStockThreshold: String(item?.lowStockThreshold ?? 0),
      isActive: item?.isActive ?? true,
    },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: InventoryItemFormValues) => {
    const body = {
      code: values.code,
      name: values.name,
      unit: values.unit,
      lowStockThreshold: Number(values.lowStockThreshold),
      isActive: values.isActive,
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (item) {
      updateItem.mutate({ id: item.inventoryItemId, body }, options);
    } else {
      createItem.mutate(body, options);
    }
  };

  return (
    <form id="inventory-item-form" onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <FieldGroup disabled={isPending}>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormInputField
            name="code"
            control={form.control}
            label="Mã sản phẩm kho"
            placeholder="BAP-NGOT"
            autoComplete="off"
            autoFocus
          />
          <FormInputField
            name="unit"
            control={form.control}
            label="Đơn vị tính"
            placeholder="kg, lít, hộp, ly..."
            autoComplete="off"
          />
        </div>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên sản phẩm kho"
          placeholder="Bắp rang vị ngọt"
          autoComplete="off"
        />
        <FormInputField
          name="lowStockThreshold"
          control={form.control}
          label="Ngưỡng cảnh báo sắp hết"
          type="number"
          min={0}
          step={INVENTORY_QUANTITY_STEP}
        />
        <FormSwitchField name="isActive" control={form.control} label="Đang sử dụng" />
      </FieldGroup>

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Thêm sản phẩm kho"}
        onCancel={onSuccess}
      />
    </form>
  );
};
