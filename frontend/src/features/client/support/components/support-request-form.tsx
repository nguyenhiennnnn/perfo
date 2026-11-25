import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { FormActions } from "@/components/common/form/form-actions";
import { FormEnumSelectField } from "@/components/common/form/form-enum-select-field";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FieldGroup } from "@/components/ui/field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import {
  EMPTY_SUPPORT_REQUEST,
  createSupportRequestSchema,
  type CreateSupportRequestFormValues,
} from "../utils/support-form";
import { supportTypeOptions } from "../utils/support-labels";

interface SupportRequestFormProps {
  defaultType?: string;
  isPending: boolean;
  onSubmit: (
    values: CreateSupportRequestFormValues,
    onServerError: (error: unknown) => boolean,
  ) => void;
  onCancel: () => void;
}

export const SupportRequestForm = ({
  defaultType = "",
  isPending,
  onSubmit,
  onCancel,
}: SupportRequestFormProps) => {
  const form = useForm<CreateSupportRequestFormValues>({
    resolver: zodResolver(createSupportRequestSchema),
    mode: "onTouched",
    defaultValues: { ...EMPTY_SUPPORT_REQUEST, type: defaultType },
  });
  const applyServerErrors = useServerFieldErrors(form);

  return (
    <form
      onSubmit={form.handleSubmit((values) => onSubmit(values, applyServerErrors))}
      className="space-y-5 pb-6"
    >
      <FieldGroup disabled={isPending} className="gap-4">
        <FormEnumSelectField
          name="type"
          control={form.control}
          label="Loại yêu cầu"
          options={supportTypeOptions}
          placeholder="Chọn loại yêu cầu"
          className="w-full"
        />
        <FormInputField
          name="subject"
          control={form.control}
          label="Tiêu đề"
          placeholder="Tóm tắt vấn đề của bạn"
          autoComplete="off"
        />
        <FormTextareaField
          name="content"
          control={form.control}
          label="Nội dung"
          placeholder="Mô tả chi tiết để rạp hỗ trợ bạn nhanh hơn"
          rows={5}
        />
      </FieldGroup>

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Gửi yêu cầu"
        onCancel={onCancel}
      />
    </form>
  );
};
