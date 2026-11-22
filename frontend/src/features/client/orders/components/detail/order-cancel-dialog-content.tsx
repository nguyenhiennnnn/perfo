import { useState, type FormEvent } from "react";
import { CheckCircle2 } from "lucide-react";
import { Chip, ChipGroup } from "@/components/common/chip";
import { FormErrorAlert } from "@/components/common/form/form-error-alert";
import { SubmitButton } from "@/components/common/form/submit-button";
import { Button } from "@/components/ui/button";
import { useBlockDialogClose } from "@/hooks/use-block-dialog-close";
import { formatCurrency } from "@/lib/formatters";
import { useDialogClose } from "@/stores/dialog-store";
import type { CancelReason } from "@/types/enum";
import { useCancelOrder } from "../../hooks/use-orders";
import type { CancelOrderResponse, OrderDetailDto } from "../../types";
import { CUSTOMER_CANCEL_REASONS, getRefundItemStatusLabel, type OrderCancelMode } from "../../utils/order-cancel";
import { getCancelReasonLabel, getPaymentMethodLabel } from "../../utils/order-labels";

const REFUND_SUMMARY_COPY = {
  None: "Đơn chưa thanh toán nên không phát sinh khoản hoàn.",
  Refunded: "Khoản hoàn đã được gửi về phương thức bạn đã thanh toán.",
  Pending: "Một phần khoản hoàn đang được xử lý. Bạn có thể theo dõi trong sổ giao dịch của đơn.",
} as const;

const CancelResult = ({ result }: { result: CancelOrderResponse }) => {
  const close = useDialogClose();

  return (
    <div className="space-y-4 pb-6" data-testid="order-cancel-result">
      <div className="flex items-start gap-3">
        <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-success" />
        <div>
          <p className="font-medium text-foreground">Đã hủy đơn {result.orderCode}</p>
          <p className="mt-1 text-sm text-muted-foreground">{REFUND_SUMMARY_COPY[result.refundStatus]}</p>
        </div>
      </div>

      {result.refunds.length > 0 && (
        <dl className="divide-y divide-border cut-sm bg-secondary px-4">
          {result.refunds.map((refund) => (
            <div key={`${refund.method}-${refund.status}`} className="flex items-start justify-between gap-3 py-3">
              <div className="min-w-0">
                <dt className="text-sm font-medium text-foreground">{getPaymentMethodLabel(refund.method)}</dt>
                <dd className="mt-0.5 text-xs text-muted-foreground">{getRefundItemStatusLabel(refund.status)}</dd>
              </div>
              <dd className="shrink-0 text-sm font-medium text-foreground tabular-nums">
                {formatCurrency(refund.amount)}
              </dd>
            </div>
          ))}
          <div className="flex items-center justify-between gap-3 py-3">
            <dt className="text-sm text-muted-foreground">Đã hoàn</dt>
            <dd data-testid="order-cancel-total-refunded" className="text-sm font-semibold text-primary-text tabular-nums">
              {formatCurrency(result.totalRefunded)}
            </dd>
          </div>
        </dl>
      )}

      <div className="flex justify-end">
        <Button onClick={close}>Đóng</Button>
      </div>
    </div>
  );
};

interface OrderCancelDialogContentProps {
  order: OrderDetailDto;
  mode: Extract<OrderCancelMode, "unpaid" | "refundable">;
}

export const OrderCancelDialogContent = ({ order, mode }: OrderCancelDialogContentProps) => {
  const close = useDialogClose();
  const cancelOrder = useCancelOrder({ inlineError: true });
  const [reason, setReason] = useState<CancelReason | null>(null);
  const [result, setResult] = useState<CancelOrderResponse | null>(null);

  useBlockDialogClose(cancelOrder.isPending);

  if (result) return <CancelResult result={result} />;

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!reason) return;
    cancelOrder.mutate(
      { orderId: order.orderId, reason },
      { onSuccess: (response) => setResult(response.data) },
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 pb-6">
      <p className="text-sm text-muted-foreground">
        {mode === "unpaid"
          ? "Đơn chưa thanh toán nên khi hủy sẽ không có khoản hoàn tiền."
          : "Số tiền hoàn được tính trên số tiền bạn đã thanh toán, theo chính sách của rạp. Thao tác này không thể hoàn tác."}
      </p>

      <div>
        <p id="cancel-reason-label" className="text-sm font-medium text-foreground">
          Lý do hủy
        </p>
        <ChipGroup role="radiogroup" aria-labelledby="cancel-reason-label" className="mt-2 flex-wrap">
          {CUSTOMER_CANCEL_REASONS.map((item) => (
            <Chip
              key={item}
              role="radio"
              selected={reason === item}
              disabled={cancelOrder.isPending}
              onClick={() => setReason(item)}
            >
              {getCancelReasonLabel(item)}
            </Chip>
          ))}
        </ChipGroup>
      </div>

      <FormErrorAlert error={cancelOrder.error} preferServerMessage />

      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Button type="button" variant="outline" onClick={close} disabled={cancelOrder.isPending}>
          Giữ lại đơn
        </Button>
        <SubmitButton
          variant="destructive"
          isPending={cancelOrder.isPending}
          disabled={!reason}
          label="Xác nhận hủy đơn"
          pendingLabel="Đang hủy"
        />
      </div>
    </form>
  );
};
