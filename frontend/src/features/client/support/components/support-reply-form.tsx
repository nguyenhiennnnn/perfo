import { zodResolver } from "@hookform/resolvers/zod";
import { Send } from "lucide-react";
import { useForm } from "react-hook-form";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { SubmitButton } from "@/components/common/form/submit-button";
import { FieldGroup } from "@/components/ui/field";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { useReplySupportRequest } from "../hooks/use-support-requests";
import {
  replySupportRequestSchema,
  toReplySupportRequest,
  type ReplySupportRequestFormValues,
} from "../utils/support-form";

interface SupportReplyFormProps {
  requestId: number;
}

export const SupportReplyForm = ({ requestId }: SupportReplyFormProps) => {
  const reply = useReplySupportRequest(requestId);
  const form = useForm<ReplySupportRequestFormValues>({
    resolver: zodResolver(replySupportRequestSchema),
    mode: "onTouched",
    defaultValues: { content: "" },
  });
  const applyServerErrors = useServerFieldErrors(form);

  return (
    <form
      onSubmit={form.handleSubmit((values) =>
        reply.mutate(toReplySupportRequest(values), {
          onSuccess: () => form.reset({ content: "" }),
          onError: applyServerErrors,
        }),
      )}
      className="space-y-3 cut-md bg-card p-4"
      data-testid="support-reply-form"
    >
      <FieldGroup disabled={reply.isPending} className="gap-3">
        <FormTextareaField
          name="content"
          control={form.control}
          label="Trả lời"
          placeholder="Nhập nội dung trả lời rạp"
          rows={3}
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
