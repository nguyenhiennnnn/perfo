import { zodResolver } from "@hookform/resolvers/zod";
import { Send } from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { FieldGroup } from "@/components/ui/field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { SupportRequestStatus } from "@/types/enum";
import { SUPPORT_MESSAGE_MAX_LENGTH } from "../constants";
import { useStaffReplySupportRequest } from "../hooks/use-admin-support";
import { SUPPORT_SETTABLE_STATUS_OPTIONS } from "../utils/support-labels";

const KEEP_STATUS = "keep";

const schema = z.object({
  content: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập nội dung trả lời.")
    .max(SUPPORT_MESSAGE_MAX_LENGTH, `Nội dung tối đa ${SUPPORT_MESSAGE_MAX_LENGTH} ký tự.`),
  status: z.string(),
});

type FormValues = z.infer<typeof schema>;

const STATUS_OPTIONS = [
  { value: KEEP_STATUS, label: "Giữ nguyên trạng thái" },
  ...SUPPORT_SETTABLE_STATUS_OPTIONS,
];

interface StaffReplyFormProps {
  requestId: number;
  status: SupportRequestStatus;
}

export const StaffReplyForm = ({ requestId, status }: StaffReplyFormProps) => {
  const reply = useStaffReplySupportRequest(requestId);
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onTouched",
    defaultValues: { content: "", status: KEEP_STATUS },
  });
  const applyServerErrors = useServerFieldErrors(form);

  return (
    <form
      onSubmit={form.handleSubmit((values) =>
        reply.mutate(
          {
            content: values.content.trim(),
            status:
              values.status === KEEP_STATUS ? null : (values.status as SupportRequestStatus),
          },
          {
            onSuccess: () => form.reset({ content: "", status: KEEP_STATUS }),
            onError: applyServerErrors,
          },
        ),
      )}
      className="space-y-3 cut-md bg-card p-4"
      data-testid="staff-support-reply-form"
    >
      <FieldGroup disabled={reply.isPending} className="gap-3">
        <FormTextareaField
          name="content"
          control={form.control}
          label="Trả lời khách"
          placeholder="Nội dung sẽ được gửi tới khách qua email."
          rows={4}
        />
        <FormSelectField
          name="status"
          control={form.control}
          label="Sau khi gửi"
          options={STATUS_OPTIONS.filter((option) => option.value !== status)}
          getOptionValue={(option) => option.value}
          getOptionLabel={(option) => option.label}
          placeholder="Giữ nguyên trạng thái"
        />
      </FieldGroup>
      <div className="flex justify-end">
        <SubmitButton
          label="Gửi trả lời"
          pendingLabel="Đang gửi"
          isPending={reply.isPending}
          icon={<Send className="size-4" />}
        />
      </div>
    </form>
  );
};
