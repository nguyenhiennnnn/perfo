import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { FormActions } from "@/components/common/form/form-actions";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FieldGroup } from "@/components/ui/field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import {
  EMPTY_INVOICE_INFO,
  invoiceInfoSchema,
  toInvoiceInfoRequest,
  type InvoiceInfoFormValues,
} from "../utils/invoice-form";
import type { UpsertInvoiceProfileRequest } from "../types";

interface InvoiceInfoFormProps {
  defaultValues?: InvoiceInfoFormValues;
  isPending: boolean;
  submitLabel: string;
  onSubmit: (
    body: UpsertInvoiceProfileRequest,
    onServerError: (error: unknown) => boolean,
  ) => void;
  onCancel: () => void;
}

export const InvoiceInfoForm = ({
  defaultValues = EMPTY_INVOICE_INFO,
  isPending,
  submitLabel,
  onSubmit,
  onCancel,
}: InvoiceInfoFormProps) => {
  const form = useForm<InvoiceInfoFormValues>({
    resolver: zodResolver(invoiceInfoSchema),
    mode: "onTouched",
    defaultValues,
  });
  const applyServerErrors = useServerFieldErrors(form);

  return (
    <form
      onSubmit={form.handleSubmit((values) => onSubmit(toInvoiceInfoRequest(values), applyServerErrors))}
      className="space-y-5 pb-6"
    >
      <FieldGroup disabled={isPending} className="gap-4">
        <FormInputField name="taxCode" control={form.control} label="Mã số thuế" autoComplete="off" autoFocus />
        <FormInputField name="companyName" control={form.control} label="Tên đơn vị" autoComplete="organization" />
        <FormTextareaField name="address" control={form.control} label="Địa chỉ" rows={3} />
        <FormInputField
          name="email"
          control={form.control}
          type="email"
          label="Email nhận hóa đơn"
          autoComplete="off"
        />
      </FieldGroup>

      <FormActions
        isPending={isPending}
        isDirty={form.formState.isDirty}
        submitLabel={submitLabel}
        onCancel={onCancel}
      />
    </form>
  );
};
