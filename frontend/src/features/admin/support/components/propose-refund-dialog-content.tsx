import { zodResolver } from "@hookform/resolvers/zod";
import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { QueryState } from "@/components/common/query-state";
import { FormActions } from "@/components/common/form/form-actions";
import { FormInputField } from "@/components/common/form/form-input-field";
import { FormSelectField } from "@/components/common/form/form-select-field";
import { FormTextareaField } from "@/components/common/form/form-textarea-field";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { FieldGroup } from "@/components/ui/field";
import { useAdminOrderDetail } from "@/features/admin/orders/hooks/use-admin-orders";
import type { OrderDetailDto } from "@/features/admin/orders/types";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { formatCurrency } from "@/lib/formatters";
import { useDialogClose } from "@/stores/dialog-store";
import { REFUND_REASON_MAX_LENGTH } from "../constants";
import { useProposeRefund } from "../hooks/use-refund-proposals";

const WHOLE_ORDER = "all";

const createSchema = (maxAmount: number) =>
  z.object({
    orderLineId: z.string(),
    amount: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập số tiền hoàn.")
      .refine((value) => /^\d+$/.test(value) && Number(value) > 0, "Số tiền hoàn phải là số nguyên dương.")
      .refine(
        (value) => Number(value) <= maxAmount,
        `Số tiền hoàn tối đa ${formatCurrency(maxAmount)}.`,
      ),
    reason: z
      .string()
      .trim()
      .min(1, "Vui lòng nhập lý do hoàn tiền.")
      .max(REFUND_REASON_MAX_LENGTH, `Lý do tối đa ${REFUND_REASON_MAX_LENGTH} ký tự.`),
  });

type FormValues = z.infer<ReturnType<typeof createSchema>>;

interface LineOption {
  value: string;
  label: string;
  refundable: number;
}

const ProposeRefundForm = ({ order }: { order: OrderDetailDto }) => {
  const close = useDialogClose();
  const propose = useProposeRefund();
  const orderRefundable = Math.max(order.payment.netPaidAmount - order.payment.pendingRefundAmount, 0);

  const lineOptions = useMemo<LineOption[]>(
    () => [
      { value: WHOLE_ORDER, label: "Toàn đơn", refundable: orderRefundable },
      ...order.lines
        .filter((line) => line.paidAmount - line.refundedAmount > 0)
        .map((line) => ({
          value: String(line.orderLineId),
          label: line.description ?? `Dòng #${line.orderLineId}`,
          refundable: Math.min(line.paidAmount - line.refundedAmount, orderRefundable),
        })),
    ],
    [order.lines, orderRefundable],
  );

  const form = useForm<FormValues>({
    resolver: (values, context, options) =>
      zodResolver(
        createSchema(lineOptions.find((o) => o.value === values.orderLineId)?.refundable ?? orderRefundable),
      )(values, context, options),
    mode: "onTouched",
    defaultValues: { orderLineId: WHOLE_ORDER, amount: "", reason: "" },
  });
  const applyServerErrors = useServerFieldErrors(form);

  if (orderRefundable <= 0)
    return (
      <p className="pb-6 text-sm text-muted-foreground">
        Đơn {order.orderCode} không còn số tiền nào có thể hoàn.
      </p>
    );

  return (
    <form
      onSubmit={form.handleSubmit((values) =>
        propose.mutate(
          {
            orderId: order.orderId,
            orderLineId: values.orderLineId === WHOLE_ORDER ? null : Number(values.orderLineId),
            amount: Number(values.amount),
            reason: values.reason.trim(),
          },
          { onSuccess: close, onError: applyServerErrors },
        ),
      )}
      className="space-y-5 pb-6"
    >
      <div className="cut-sm bg-muted px-3 py-2 text-sm">
        <p className="text-muted-foreground">
          Đã thu thuần:{" "}
          <span className="font-medium text-foreground tabular-nums">{formatCurrency(order.payment.netPaidAmount)}</span>
        </p>
        <p className="text-muted-foreground">
          Có thể hoàn tối đa:{" "}
          <span className="font-medium text-foreground tabular-nums">{formatCurrency(orderRefundable)}</span>
        </p>
      </div>
      <FieldGroup disabled={propose.isPending} className="gap-4">
        <FormSelectField
          name="orderLineId"
          control={form.control}
          label="Hoàn cho"
          options={lineOptions}
          getOptionValue={(option) => option.value}
          getOptionLabel={(option) => option.label}
        />
        <FormInputField
          name="amount"
          control={form.control}
          label="Số tiền hoàn (VND)"
          type="number"
          min={1}
          step={1}
          autoFocus
        />
        <FormTextareaField
          name="reason"
          control={form.control}
          label="Lý do hoàn ngoại lệ"
          placeholder="Ví dụ: Khách bị trừ tiền hai lần do lỗi cổng thanh toán."
          rows={4}
        />
      </FieldGroup>
      <p className="text-sm text-muted-foreground">
        Đề xuất cần quản trị khác duyệt trước khi hoàn. Đề xuất vượt ngưỡng cấu hình phải kèm ghi chú duyệt.
        Hoàn tiền không tự hủy vé; nếu cần nhả ghế, quản trị chọn hủy vé khi duyệt.
      </p>
      <FormActions
        isPending={propose.isPending}
        isDirty={form.formState.isDirty}
        submitLabel="Gửi đề xuất"
        onCancel={close}
      />
    </form>
  );
};

interface ProposeRefundDialogContentProps {
  orderId: number;
}

export const ProposeRefundDialogContent = ({ orderId }: ProposeRefundDialogContentProps) => {
  const orderQuery = useAdminOrderDetail(orderId);

  return (
    <QueryState
      query={orderQuery}
      skeleton={<CardSkeleton className="h-48" />}
      errorTitle="Không tải được đơn hàng"
    >
      {(order) => <ProposeRefundForm order={order} />}
    </QueryState>
  );
};
