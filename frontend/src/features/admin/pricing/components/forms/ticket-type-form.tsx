import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { Button } from "@/components/ui/button";
import { Field, FieldDescription, FieldGroup, FieldLabel } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormTimePickerField } from "@/components/common/form/form-time-picker-field";
import { FormMultiSelectField } from "@/components/common/form/form-search-select-field";
import { FormActions } from "@/components/common/form/form-actions";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useAdminAgeRatings } from "@/features/admin/catalog/hooks/use-admin-age-rating";
import { FORM_SAVE_CHANGES_LABEL, LOOKUP_PAGE_SIZE, NAME_MAX_LENGTH } from "@/config/default-constants";

import {
  useAdminCreateTicketType,
  useAdminUpdateTicketType,
} from "../../hooks/use-admin-ticket-types";
import type { TicketTypeDto } from "../../types";
import { DAY_OF_WEEK_OPTIONS } from "../../utils/pricing-labels";

const MIN_MULTIPLIER = 0.01;
const MAX_MULTIPLIER = 5;

const ticketTypeFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tên loại vé.")
      .max(NAME_MAX_LENGTH, `Tên loại vé tối đa ${NAME_MAX_LENGTH} ký tự.`),
    priceMultiplier: z.coerce
      .number({ message: "Hệ số giá phải là số." })
      .min(MIN_MULTIPLIER, `Hệ số giá tối thiểu là ${MIN_MULTIPLIER}.`)
      .max(MAX_MULTIPLIER, `Hệ số giá tối đa là ${MAX_MULTIPLIER}.`),
    requiresCardVerification: z.boolean(),
    allowedAgeRatingIds: z.array(z.number()),
    allowedFromTime: z.string(),
    allowedToTime: z.string(),
    allowedDaysOfWeek: z.array(z.number()),
    isActive: z.boolean(),
  })
  .superRefine((values, ctx) => {
    const hasFrom = Boolean(values.allowedFromTime);
    const hasTo = Boolean(values.allowedToTime);
    if (hasFrom !== hasTo) {
      ctx.addIssue({
        code: "custom",
        path: [hasFrom ? "allowedToTime" : "allowedFromTime"],
        message: "Cần nhập cả giờ bắt đầu và giờ kết thúc.",
      });
      return;
    }
    if (hasFrom && values.allowedFromTime >= values.allowedToTime) {
      ctx.addIssue({
        code: "custom",
        path: ["allowedToTime"],
        message: "Giờ kết thúc phải sau giờ bắt đầu.",
      });
    }
  });

type TicketTypeFormValues = z.infer<typeof ticketTypeFormSchema>;

interface TicketTypeFormProps {
  ticketType?: TicketTypeDto;
  onSuccess: () => void;
}

export const TicketTypeForm = ({ ticketType, onSuccess }: TicketTypeFormProps) => {
  const isEdit = Boolean(ticketType);
  const createTicketType = useAdminCreateTicketType();
  const updateTicketType = useAdminUpdateTicketType();
  const isPending = createTicketType.isPending || updateTicketType.isPending;

  const [ageRatingSearch, setAgeRatingSearch] = useState("");
  const ageRatings = useAdminAgeRatings({ pageSize: LOOKUP_PAGE_SIZE });

  const form = useForm<TicketTypeFormValues>({
    resolver: zodResolver(ticketTypeFormSchema),
    defaultValues: {
      name: ticketType?.name ?? "",
      priceMultiplier: ticketType?.priceMultiplier ?? 1,
      requiresCardVerification: ticketType?.requiresCardVerification ?? false,
      allowedAgeRatingIds: ticketType?.allowedAgeRatingIds ?? [],
      allowedFromTime: ticketType?.allowedFromTime ?? "",
      allowedToTime: ticketType?.allowedToTime ?? "",
      allowedDaysOfWeek: ticketType?.allowedDaysOfWeek ?? [],
      isActive: ticketType?.isActive ?? true,
    },
  });

  const applyServerErrors = useServerFieldErrors(form);
  const selectedDays = form.watch("allowedDaysOfWeek");

  const toggleDay = (day: number) => {
    const next = selectedDays.includes(day)
      ? selectedDays.filter((d) => d !== day)
      : [...selectedDays, day];
    form.setValue("allowedDaysOfWeek", next, { shouldDirty: true });
  };

  const onSubmit = (values: TicketTypeFormValues) => {
    const body = {
      name: values.name,
      priceMultiplier: values.priceMultiplier,
      requiresCardVerification: values.requiresCardVerification,
      allowedAgeRatingIds: values.allowedAgeRatingIds,
      allowedFromTime: values.allowedFromTime || null,
      allowedToTime: values.allowedToTime || null,
      allowedDaysOfWeek: values.allowedDaysOfWeek,
      isActive: values.isActive,
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (ticketType) {
      updateTicketType.mutate({ id: ticketType.ticketTypeId, body }, options);
    } else {
      createTicketType.mutate(body, options);
    }
  };

  return (
    <form
      id="ticket-type-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="max-h-[70vh] space-y-4 overflow-y-auto px-0.5"
    >
      <FieldGroup disabled={isPending}>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên loại vé"
          placeholder="Người lớn, Học sinh - sinh viên..."
          autoComplete="off"
          autoFocus
        />

        <FormInputField
          name="priceMultiplier"
          control={form.control}
          label="Hệ số giá"
          type="number"
          min={MIN_MULTIPLIER}
          max={MAX_MULTIPLIER}
          step={0.05}
        />

        <FormSwitchField
          name="requiresCardVerification"
          control={form.control}
          label="Yêu cầu xuất trình thẻ khi vào rạp"
        />

        <FormMultiSelectField
          name="allowedAgeRatingIds"
          control={form.control}
          label="Giới hạn độ tuổi phim (để trống là mọi phim)"
          options={ageRatings.data?.items ?? []}
          selectedOptions={ageRatings.data?.items ?? []}
          getOptionValue={(o) => o.ageRatingId}
          getOptionLabel={(o) => o.code}
          searchValue={ageRatingSearch}
          onSearchChange={setAgeRatingSearch}
          isLoading={ageRatings.isLoading}
          placeholder="Mọi độ tuổi"
        />

        <div className="grid grid-cols-2 gap-3">
          <FormTimePickerField
            name="allowedFromTime"
            control={form.control}
            label="Từ giờ"
          />
          <FormTimePickerField
            name="allowedToTime"
            control={form.control}
            label="Đến giờ"
          />
        </div>

        <Field>
          <FieldLabel>Các thứ được áp dụng (để trống là mọi ngày)</FieldLabel>
          <div className="flex flex-wrap gap-1.5">
            {DAY_OF_WEEK_OPTIONS.map((day) => (
              <Button
                key={day.value}
                type="button"
                size="sm"
                variant={selectedDays.includes(day.value) ? "default" : "outline"}
                onClick={() => toggleDay(day.value)}
              >
                {day.label}
              </Button>
            ))}
          </div>
          <FieldDescription>
            Khung giờ tính theo giờ chiếu của suất, ngày tính theo ngày chiếu.
          </FieldDescription>
        </Field>

        <FormSwitchField name="isActive" control={form.control} label="Đang hoạt động" />
      </FieldGroup>

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Thêm loại vé"}
        onCancel={onSuccess}
      />
    </form>
  );
};
