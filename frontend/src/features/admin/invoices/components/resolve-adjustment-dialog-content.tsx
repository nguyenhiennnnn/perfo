import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { FormActions } from "@/components/common/form/form-actions";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FieldGroup } from "@/components/ui/field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { formatCurrency } from "@/lib/formatters";
import { useDialogClose } from "@/stores/dialog-store";
import { INVOICE_REASON_MAX_LENGTH } from "../constants";
import { useResolveInvoiceAdjustment } from "../hooks/use-admin-invoices";

const schema = z.object({
  note: z.string().trim().max(INVOICE_REASON_MAX_LENGTH, `Ghi chú tối đa ${INVOICE_REASON_MAX_LENGTH} ký tự.`),
});

type FormValues = z.infer<typeof schema>;

interface ResolveAdjustmentDialogContentProps {
  requestId: number;
  refundAmount: number;
}

export const ResolveAdjustmentDialogContent = ({
  requestId,
  refundAmount,
}: ResolveAdjustmentDialogContentProps) => {
  const close = useDialogClose();
  const resolve = useResolveInvoiceAdjustment(requestId);
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onTouched",
    defaultValues: { note: "" },
  });
  const applyServerErrors = useServerFieldErrors(form);

  return (
    <form
      onSubmit={form.handleSubmit((values) =>
        resolve.mutate(
          { note: values.note.trim() || null },
          { onSuccess: close, onError: applyServerErrors },
        ),
      )}
      className="space-y-5 pb-6"
    >
      <p className="text-sm text-muted-foreground">
        Đơn đã hoàn {formatCurrency(refundAmount)} sau khi xuất hóa đơn. Xác nhận khi bạn đã điều chỉnh hoặc thay thế
        hóa đơn với đơn vị.
      </p>
      <FieldGroup disabled={resolve.isPending}>
        <FormTextareaField
          name="note"
          control={form.control}
          label="Ghi chú xử lý (không bắt buộc)"
          placeholder="Ví dụ: Đã xuất hóa đơn điều chỉnh số 0000123."
          rows={3}
        />
      </FieldGroup>
      <FormActions
        isPending={resolve.isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Đánh dấu đã xử lý"
        onCancel={close}
      />
    </form>
  );
};
