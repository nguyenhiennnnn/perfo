import { Field, FieldDescription, FieldError, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { INVOICE_FILE_MAX_BYTES } from "../constants";

export const validateInvoiceFile = (file: File | null, required: boolean): string | null => {
  if (!file) return required ? "Vui lòng chọn tệp hóa đơn PDF." : null;
  if (!file.name.toLowerCase().endsWith(".pdf")) return "Chỉ chấp nhận tệp PDF.";
  if (file.size <= 0) return "Tệp rỗng.";
  if (file.size > INVOICE_FILE_MAX_BYTES) {
    return `Tệp tối đa ${INVOICE_FILE_MAX_BYTES / (1024 * 1024)} MB.`;
  }
  return null;
};

interface InvoiceFilePickerProps {
  file: File | null;
  error: string | null;
  required?: boolean;
  disabled?: boolean;
  onChange: (file: File | null) => void;
}

export const InvoiceFilePicker = ({
  file,
  error,
  required = false,
  disabled,
  onChange,
}: InvoiceFilePickerProps) => (
  <Field data-invalid={Boolean(error)}>
    <FieldLabel htmlFor="invoice-file">Tệp hóa đơn (PDF){required ? "" : " - không bắt buộc"}</FieldLabel>
    <Input
      id="invoice-file"
      type="file"
      accept="application/pdf,.pdf"
      disabled={disabled}
      aria-invalid={Boolean(error)}
      onChange={(event) => onChange(event.target.files?.[0] ?? null)}
    />
    {file && !error && <FieldDescription>{file.name}</FieldDescription>}
    {error && <FieldError>{error}</FieldError>}
  </Field>
);
