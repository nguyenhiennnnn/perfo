import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { FormActions } from "@/components/common/form/form-actions";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { FieldGroup } from "@/components/ui/field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useDialogClose } from "@/stores/dialog-store";
import { REFUND_REASON_MAX_LENGTH } from "../constants";
import { useRejectRefundProposal } from "../hooks/use-refund-proposals";

const schema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập lý do từ chối.")
    .max(REFUND_REASON_MAX_LENGTH, `Lý do tối đa ${REFUND_REASON_MAX_LENGTH} ký tự.`),
});

type FormValues = z.infer<typeof schema>;

export const RejectRefundDialogContent = ({ proposalId }: { proposalId: number }) => {
  const close = useDialogClose();
  const reject = useRejectRefundProposal(proposalId);
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onTouched",
    defaultValues: { reason: "" },
  });
  const applyServerErrors = useServerFieldErrors(form);

  return (
    <form
      onSubmit={form.handleSubmit((values) =>
        reject.mutate({ reason: values.reason.trim() }, { onSuccess: close, onError: applyServerErrors }),
      )}
      className="space-y-5 pb-6"
    >
      <FieldGroup disabled={reject.isPending}>
        <FormTextareaField
          name="reason"
          control={form.control}
          label="Lý do từ chối"
          placeholder="Ví dụ: Đơn đã được hoàn theo chính sách, không đủ căn cứ hoàn thêm."
          rows={4}
          autoFocus
        />
      </FieldGroup>
      <FormActions
        isPending={reject.isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Từ chối đề xuất"
        onCancel={close}
      />
    </form>
  );
};
