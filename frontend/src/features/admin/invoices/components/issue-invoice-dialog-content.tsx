import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { FormActions } from "@/components/common/form/form-actions";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FieldGroup } from "@/components/ui/field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useDialogClose } from "@/stores/dialog-store";
import { INVOICE_NUMBER_MAX_LENGTH } from "../constants";
import { useIssueInvoice } from "../hooks/use-admin-invoices";
import { InvoiceFilePicker, validateInvoiceFile } from "./invoice-file-picker";

const schema = z.object({
  invoiceNumber: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập số hóa đơn.")
    .max(INVOICE_NUMBER_MAX_LENGTH, `Số hóa đơn tối đa ${INVOICE_NUMBER_MAX_LENGTH} ký tự.`),
});

type FormValues = z.infer<typeof schema>;

interface IssueInvoiceDialogContentProps {
  requestId: number;
}

export const IssueInvoiceDialogContent = ({ requestId }: IssueInvoiceDialogContentProps) => {
  const close = useDialogClose();
  const issue = useIssueInvoice(requestId);
  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onTouched",
    defaultValues: { invoiceNumber: "" },
  });
  const applyServerErrors = useServerFieldErrors(form);

  const handleFileChange = (next: File | null) => {
    setFile(next);
    setFileError(validateInvoiceFile(next, false));
  };

  const onSubmit = (values: FormValues) => {
    const error = validateInvoiceFile(file, false);
    setFileError(error);
    if (error) return;
    issue.mutate(
      { invoiceNumber: values.invoiceNumber.trim(), file },
      { onSuccess: close, onError: applyServerErrors },
    );
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5 pb-6">
      <FieldGroup disabled={issue.isPending} className="gap-4">
        <FormInputField
          name="invoiceNumber"
          control={form.control}
          label="Số hóa đơn"
          autoComplete="off"
          autoFocus
        />
        <InvoiceFilePicker file={file} error={fileError} disabled={issue.isPending} onChange={handleFileChange} />
      </FieldGroup>
      <p className="text-sm text-muted-foreground">
        Khi xác nhận, yêu cầu chuyển sang đã xuất và khách nhận email thông báo. Khách chỉ tải được hóa đơn nếu có tệp
        đính kèm.
      </p>
      <FormActions
        isPending={issue.isPending}
        isDirty={form.formState.isDirty || file !== null}
        submitLabel="Xác nhận đã xuất"
        onCancel={close}
      />
    </form>
  );
};
