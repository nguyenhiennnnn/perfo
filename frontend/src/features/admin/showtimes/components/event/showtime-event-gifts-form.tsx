import { Plus, Trash2 } from "lucide-react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { Button } from "@/components/ui/button";
import { FieldGroup } from "@/components/ui/field";
import { useAdminInventoryItemOptions } from "@/features/admin/inventory/hooks/use-admin-inventory";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";

import { useUpsertShowtimeEventGifts } from "../../hooks/use-showtime-event";
import type { ShowtimeEventGiftsDto } from "../../types";

const schema = z.object({
  gifts: z.array(
    z.object({
      inventoryItemId: z.coerce.number({ message: "Vui lòng chọn quà." }).min(1, "Vui lòng chọn quà."),
      quantity: z.coerce.number().int("Nhập số nguyên.").min(1, "Tối thiểu 1."),
      quantityPerTicket: z.coerce.number().int("Nhập số nguyên.").min(1, "Tối thiểu 1."),
    }),
  ),
});

type FormValues = z.infer<typeof schema>;

interface ShowtimeEventGiftsFormProps {
  showtimeId: number;
  gifts: ShowtimeEventGiftsDto;
}

export const ShowtimeEventGiftsForm = ({ showtimeId, gifts }: ShowtimeEventGiftsFormProps) => {
  const upsert = useUpsertShowtimeEventGifts();
  const items = useAdminInventoryItemOptions();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      gifts: gifts.gifts.map((g) => ({
        inventoryItemId: g.inventoryItemId,
        quantity: g.quantity,
        quantityPerTicket: g.quantityPerTicket,
      })),
    },
  });
  const { fields, append, remove } = useFieldArray({ control: form.control, name: "gifts" });
  const applyServerErrors = useServerFieldErrors(form);

  const progress = new Map(gifts.gifts.map((g) => [g.inventoryItemId, g]));

  const onSubmit = (values: FormValues) =>
    upsert.mutate({ showtimeId, body: values }, { onError: applyServerErrors });

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <div className="flex items-center justify-between">
        <h4 className="text-sm font-medium">Quà tặng kèm vé</h4>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => append({ inventoryItemId: 0, quantity: 1, quantityPerTicket: 1 })}
        >
          <Plus /> Thêm quà
        </Button>
      </div>
      {fields.length === 0 && (
        <p className="text-sm text-muted-foreground">Chưa khai báo quà tặng kèm.</p>
      )}
      <FieldGroup disabled={upsert.isPending}>
        {fields.map((field, index) => {
          const row = progress.get(Number(field.inventoryItemId));
          return (
            <div key={field.id} className="grid grid-cols-[1fr_6rem_6rem_auto] items-end gap-2">
              <FormSelectField
                name={`gifts.${index}.inventoryItemId`}
                control={form.control}
                label={index === 0 ? "Sản phẩm kho" : undefined}
                placeholder="Chọn quà"
                options={items.data ?? []}
                getOptionValue={(o) => o.inventoryItemId}
                getOptionLabel={(o) => `${o.name} (${o.unit})`}
              />
              <FormInputField
                name={`gifts.${index}.quantity`}
                control={form.control}
                label={index === 0 ? "Tổng số" : "Số lượng"}
                type="number"
                min={1}
              />
              <FormInputField
                name={`gifts.${index}.quantityPerTicket`}
                control={form.control}
                label="Mỗi vé"
                type="number"
                min={1}
              />
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label="Xóa quà"
                onClick={() => remove(index)}
              >
                <Trash2 />
              </Button>
              {row && (
                <p className="col-span-4 -mt-1 text-xs text-muted-foreground">
                  Đang giữ: {row.held} · Đã trao: {row.given}
                </p>
              )}
            </div>
          );
        })}
      </FieldGroup>
      <div className="flex justify-end">
        <SubmitButton isPending={upsert.isPending} label="Lưu quà tặng" pendingLabel="Đang lưu..." />
      </div>
    </form>
  );
};
