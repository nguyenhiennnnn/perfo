import { Plus, Trash2 } from "lucide-react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { Button } from "@/components/ui/button";
import { FieldGroup } from "@/components/ui/field";
import { LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { useAdminTicketTypes } from "@/features/admin/pricing/hooks/use-admin-ticket-types";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";

import { useUpsertShowtimeEventLimits } from "../../hooks/use-showtime-event";
import type { ShowtimeEventLimitsDto } from "../../types";

const schema = z.object({
  maxTicketsPerCustomer: z
    .string()
    .refine((v) => v === "" || (/^\d+$/.test(v) && Number(v) >= 1), "Nhập số nguyên từ 1 trở lên."),
  ticketQuotas: z.array(
    z.object({
      ticketTypeId: z.coerce.number({ message: "Vui lòng chọn loại vé." }).min(1, "Vui lòng chọn loại vé."),
      maxTickets: z.coerce.number().int("Nhập số nguyên.").min(1, "Tối thiểu 1 vé."),
    }),
  ),
});

type FormValues = z.infer<typeof schema>;

interface ShowtimeEventLimitsFormProps {
  showtimeId: number;
  limits: ShowtimeEventLimitsDto;
}

export const ShowtimeEventLimitsForm = ({ showtimeId, limits }: ShowtimeEventLimitsFormProps) => {
  const upsert = useUpsertShowtimeEventLimits();
  const ticketTypes = useAdminTicketTypes({ pageSize: LOOKUP_PAGE_SIZE, isActive: true });

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      maxTicketsPerCustomer: limits.maxTicketsPerCustomer?.toString() ?? "",
      ticketQuotas: limits.ticketQuotas.map((q) => ({
        ticketTypeId: q.ticketTypeId,
        maxTickets: q.maxTickets,
      })),
    },
  });
  const { fields, append, remove } = useFieldArray({ control: form.control, name: "ticketQuotas" });
  const applyServerErrors = useServerFieldErrors(form);

  const usedByType = new Map(limits.ticketQuotas.map((q) => [q.ticketTypeId, q.usedTickets]));
  const options = ticketTypes.data?.items ?? [];

  const onSubmit = (values: FormValues) =>
    upsert.mutate(
      {
        showtimeId,
        body: {
          maxTicketsPerCustomer:
            values.maxTicketsPerCustomer === "" ? null : Number(values.maxTicketsPerCustomer),
          ticketQuotas: values.ticketQuotas,
        },
      },
      { onError: applyServerErrors },
    );

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <FieldGroup disabled={upsert.isPending}>
        <FormInputField
          name="maxTicketsPerCustomer"
          control={form.control}
          label="Số vé tối đa mỗi khách (để trống nếu không giới hạn)"
          type="number"
          min={1}
        />
      </FieldGroup>

      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h4 className="text-sm font-medium">Hạn mức theo loại vé</h4>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => append({ ticketTypeId: 0, maxTickets: 1 })}
          >
            <Plus /> Thêm loại vé
          </Button>
        </div>
        {fields.length === 0 && (
          <p className="text-sm text-muted-foreground">Chưa có hạn mức theo loại vé.</p>
        )}
        {fields.map((field, index) => {
          const used = usedByType.get(Number(field.ticketTypeId));
          return (
            <div key={field.id} className="grid grid-cols-[1fr_8rem_auto] items-end gap-2">
              <FormSelectField
                name={`ticketQuotas.${index}.ticketTypeId`}
                control={form.control}
                label={index === 0 ? "Loại vé" : undefined}
                placeholder="Chọn loại vé"
                options={options}
                getOptionValue={(o) => o.ticketTypeId}
                getOptionLabel={(o) => o.name}
              />
              <FormInputField
                name={`ticketQuotas.${index}.maxTickets`}
                control={form.control}
                label={index === 0 ? "Số vé tối đa" : "Số vé"}
                type="number"
                min={1}
              />
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label="Xóa hạn mức"
                onClick={() => remove(index)}
              >
                <Trash2 />
              </Button>
              {used !== undefined && (
                <p className="col-span-3 -mt-1 text-xs text-muted-foreground">Đã giữ/bán: {used} vé</p>
              )}
            </div>
          );
        })}
      </div>

      <div className="flex justify-end">
        <SubmitButton isPending={upsert.isPending} label="Lưu hạn mức" pendingLabel="Đang lưu..." />
      </div>
    </form>
  );
};
