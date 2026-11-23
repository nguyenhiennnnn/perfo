import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormDatePickerField } from "@/components/common/form/form-date-picker-field";
import { FormSwitchField } from "@/components/common/form/form-switch-field";
import { FormActions } from "@/components/common/form/form-actions";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { toDateInputValue } from "@/lib/formatters";
import { FORM_SAVE_CHANGES_LABEL, PERSON_NAME_MAX_LENGTH } from "@/config/default-constants";

import {
  useAdminCreateSpecialDay,
  useAdminUpdateSpecialDay,
} from "../../hooks/use-admin-special-days";
import type { SpecialDayDto } from "../../types";

const MIN_YEAR = 2000;
const MAX_YEAR = 2100;
const MAX_SPAN_DAYS = 31;
const MS_PER_DAY = 24 * 60 * 60 * 1000;

const spanInDays = (from: string, to: string) =>
  Math.round((Date.parse(to) - Date.parse(from)) / MS_PER_DAY) + 1;

const specialDayFormSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập tên ngày lễ hoặc sự kiện.")
      .max(PERSON_NAME_MAX_LENGTH, `Tên tối đa ${PERSON_NAME_MAX_LENGTH} ký tự.`),
    startDate: z.string().min(1, "Vui lòng chọn ngày bắt đầu."),
    endDate: z.string().min(1, "Vui lòng chọn ngày kết thúc."),
    isActive: z.boolean(),
  })
  .superRefine((values, ctx) => {
    if (!values.startDate || !values.endDate) return;
    const startYear = Number(values.startDate.slice(0, 4));
    const endYear = Number(values.endDate.slice(0, 4));
    if (startYear < MIN_YEAR || endYear > MAX_YEAR) {
      ctx.addIssue({
        code: "custom",
        path: ["startDate"],
        message: `Ngày phải nằm trong khoảng năm ${MIN_YEAR} đến ${MAX_YEAR}.`,
      });
      return;
    }
    if (values.endDate < values.startDate) {
      ctx.addIssue({
        code: "custom",
        path: ["endDate"],
        message: "Ngày kết thúc không được trước ngày bắt đầu.",
      });
      return;
    }
    if (spanInDays(values.startDate, values.endDate) > MAX_SPAN_DAYS) {
      ctx.addIssue({
        code: "custom",
        path: ["endDate"],
        message: `Khoảng ngày tối đa ${MAX_SPAN_DAYS} ngày.`,
      });
    }
  });

type SpecialDayFormValues = z.infer<typeof specialDayFormSchema>;

interface SpecialDayFormProps {
  specialDay?: SpecialDayDto;
  onSuccess: () => void;
}

export const SpecialDayForm = ({ specialDay, onSuccess }: SpecialDayFormProps) => {
  const isEdit = Boolean(specialDay);
  const createSpecialDay = useAdminCreateSpecialDay();
  const updateSpecialDay = useAdminUpdateSpecialDay();
  const isPending = createSpecialDay.isPending || updateSpecialDay.isPending;

  const form = useForm<SpecialDayFormValues>({
    resolver: zodResolver(specialDayFormSchema),
    defaultValues: {
      name: specialDay?.name ?? "",
      startDate: toDateInputValue(specialDay?.startDate) ?? "",
      endDate: toDateInputValue(specialDay?.endDate) ?? "",
      isActive: specialDay?.isActive ?? true,
    },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: SpecialDayFormValues) => {
    const body = {
      name: values.name,
      startDate: values.startDate,
      endDate: values.endDate,
      isActive: values.isActive,
    };
    const options = { onSuccess: () => onSuccess(), onError: applyServerErrors };

    if (specialDay) {
      updateSpecialDay.mutate({ id: specialDay.specialDayId, body }, options);
    } else {
      createSpecialDay.mutate(body, options);
    }
  };

  return (
    <form
      id="special-day-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="space-y-4"
    >
      <FieldGroup disabled={isPending}>
        <FormInputField
          name="name"
          control={form.control}
          label="Tên ngày lễ hoặc sự kiện"
          placeholder="Tết Nguyên Đán, Quốc khánh 2/9..."
          autoComplete="off"
          autoFocus
        />

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <FormDatePickerField
            name="startDate"
            control={form.control}
            label="Từ ngày"
            clearable={false}
            captionLayout="dropdown"
            fromYear={MIN_YEAR}
            toYear={MAX_YEAR}
          />
          <FormDatePickerField
            name="endDate"
            control={form.control}
            label="Đến ngày"
            clearable={false}
            captionLayout="dropdown"
            fromYear={MIN_YEAR}
            toYear={MAX_YEAR}
          />
        </div>

        <FormSwitchField name="isActive" control={form.control} label="Đang hoạt động" />
      </FieldGroup>

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Thêm ngày lễ"}
        onCancel={onSuccess}
      />
    </form>
  );
};
