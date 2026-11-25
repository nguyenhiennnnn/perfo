import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { FormActions } from "@/components/common/form/form-actions";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { Checkbox } from "@/components/ui/checkbox";
import { FieldGroup } from "@/components/ui/field";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminOrderDetail } from "@/features/admin/orders/hooks/use-admin-orders";
import type { OrderLineDto } from "@/features/admin/orders/types";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { useDialogClose } from "@/stores/dialog-store";
import { OrderLineType } from "@/types/enum";
import { REFUND_REASON_MAX_LENGTH } from "../constants";
import { useApproveRefundProposal } from "../hooks/use-refund-proposals";
import type { ExceptionalRefundProposalDto } from "../types";

const createSchema = (requiresNote: boolean) =>
  z.object({
    note: z
      .string()
      .trim()
      .max(REFUND_REASON_MAX_LENGTH, `Ghi chú tối đa ${REFUND_REASON_MAX_LENGTH} ký tự.`)
      .refine((value) => !requiresNote || value.length > 0, "Đề xuất vượt ngưỡng cần ghi chú duyệt."),
  });

type FormValues = z.infer<ReturnType<typeof createSchema>>;

const describeTicketLine = (line: OrderLineDto) => {
  const ticket = line.ticket;
  if (!ticket) return line.description ?? `Vé #${line.orderLineId}`;
  return `${ticket.movieTitle} · ${formatDateTime(ticket.startTime)} · Ghế ${ticket.seatRow}${ticket.seatNumber}`;
};

interface ApproveRefundDialogContentProps {
  proposal: ExceptionalRefundProposalDto;
}

export const ApproveRefundDialogContent = ({ proposal }: ApproveRefundDialogContentProps) => {
  const close = useDialogClose();
  const approve = useApproveRefundProposal(proposal.exceptionalRefundProposalId);
  const orderQuery = useAdminOrderDetail(proposal.orderId);
  const [cancelLineIds, setCancelLineIds] = useState<number[]>([]);
  const schema = useMemo(() => createSchema(proposal.requiresAdminApproval), [proposal.requiresAdminApproval]);
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onTouched",
    defaultValues: { note: "" },
  });
  const applyServerErrors = useServerFieldErrors(form);

  const ticketLines = useMemo(
    () =>
      (orderQuery.data?.lines ?? []).filter(
        (line) => line.lineType === OrderLineType.Ticket && line.refundedQuantity < line.quantity,
      ),
    [orderQuery.data],
  );

  const toggleLine = (lineId: number, checked: boolean) =>
    setCancelLineIds((current) =>
      checked ? [...current, lineId] : current.filter((id) => id !== lineId),
    );

  return (
    <form
      onSubmit={form.handleSubmit((values) =>
        approve.mutate(
          { note: values.note.trim() || null, cancelTicketLineIds: cancelLineIds },
          { onSuccess: close, onError: applyServerErrors },
        ),
      )}
      className="space-y-5 pb-6"
    >
      <div className="space-y-1 cut-sm bg-muted px-3 py-2 text-sm">
        <p className="text-foreground">
          Hoàn <span className="font-semibold tabular-nums">{formatCurrency(proposal.amount)}</span>
          {proposal.orderLineDescription ? ` cho ${proposal.orderLineDescription}` : " cho toàn đơn"}
        </p>
        <p className="text-muted-foreground">
          Đề xuất bởi {proposal.proposedByName} lúc {formatDateTime(proposal.proposedAt)}
        </p>
        <p className="whitespace-pre-wrap wrap-break-word text-foreground">{proposal.reason}</p>
      </div>

      <FieldGroup disabled={approve.isPending}>
        <FormTextareaField
          name="note"
          control={form.control}
          label={proposal.requiresAdminApproval ? "Ghi chú duyệt (bắt buộc)" : "Ghi chú duyệt"}
          placeholder="Căn cứ duyệt hoàn ngoại lệ."
          rows={3}
          autoFocus
        />
      </FieldGroup>

      <div className="space-y-2">
        <p className="text-sm font-medium text-foreground">Hủy vé kèm theo (nhả ghế)</p>
        {orderQuery.isLoading ? (
          <Skeleton className="h-10 w-full" />
        ) : ticketLines.length === 0 ? (
          <p className="text-sm text-muted-foreground">Đơn không có vé nào còn có thể hủy.</p>
        ) : (
          <ul className="space-y-2">
            {ticketLines.map((line) => (
              <li key={line.orderLineId}>
                <label className="flex cursor-pointer items-start gap-3 cut-sm bg-card px-3 py-2 text-sm">
                  <Checkbox
                    checked={cancelLineIds.includes(line.orderLineId)}
                    onCheckedChange={(checked) => toggleLine(line.orderLineId, checked === true)}
                    disabled={approve.isPending}
                  />
                  <span className="text-foreground">{describeTicketLine(line)}</span>
                </label>
              </li>
            ))}
          </ul>
        )}
        <p className="text-sm text-muted-foreground">
          Hoàn ngoại lệ không tự thay đổi vé. Chỉ vé được chọn mới bị hủy và nhả ghế trong thao tác này.
        </p>
      </div>

      <FormActions
        isPending={approve.isPending}
        isDirty={form.formState.isDirty || cancelLineIds.length > 0}
        submitLabel="Duyệt và hoàn tiền"
        onCancel={close}
      />
    </form>
  );
};
