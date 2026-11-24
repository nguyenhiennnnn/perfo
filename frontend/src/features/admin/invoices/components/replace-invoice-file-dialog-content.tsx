import { useState, type FormEvent } from "react";
import { FormActions } from "@/components/common/form/form-actions";
import { FieldGroup } from "@/components/ui/field";
import { useDialogClose } from "@/stores/dialog-store";
import { useReplaceInvoiceFile } from "../hooks/use-admin-invoices";
import { InvoiceFilePicker, validateInvoiceFile } from "./invoice-file-picker";

interface ReplaceInvoiceFileDialogContentProps {
  requestId: number;
}

export const ReplaceInvoiceFileDialogContent = ({ requestId }: ReplaceInvoiceFileDialogContentProps) => {
  const close = useDialogClose();
  const replace = useReplaceInvoiceFile(requestId);
  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const error = validateInvoiceFile(file, true);
    setFileError(error);
    if (error || !file) return;
    replace.mutate(file, { onSuccess: close });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5 pb-6">
      <FieldGroup disabled={replace.isPending}>
        <InvoiceFilePicker
          file={file}
          error={fileError}
          required
          disabled={replace.isPending}
          onChange={(next) => {
            setFile(next);
            setFileError(validateInvoiceFile(next, true));
          }}
        />
      </FieldGroup>
      <FormActions
        isPending={replace.isPending}
        isDirty={file !== null}
        submitLabel="Lưu tệp"
        onCancel={close}
      />
    </form>
  );
};
