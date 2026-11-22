import { Banknote, CheckCircle2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { formatCurrency } from "@/lib/formatters";
import type { CounterCancelResponse } from "../types/counter-cancel";
import {
  getCashRefundDue,
  getPaymentMethodLabel,
  getRefundStatusInfo,
  getRefundTotal,
} from "../utils/cancel-order";

const REFUND_SUMMARY: Record<CounterCancelResponse["refundStatus"], string> = {
  None: "Đơn chưa thu tiền nên không phát sinh khoản hoàn.",
  Refunded: "Các khoản hoàn đã được thực hiện xong.",
  Pending: "Còn khoản hoàn đang chờ xử lý, xem chi tiết bên dưới.",
};

interface CounterCancelResultProps {
  result: CounterCancelResponse;
}

export const CounterCancelResult = ({ result }: CounterCancelResultProps) => {
  const cashDue = getCashRefundDue(result.refunds);
  const refundTotal = getRefundTotal(result.refunds);

  return (
    <section
      data-testid="counter-cancel-result"
      className="flex flex-col gap-4"
    >
      <div className="flex items-start gap-3">
        <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-success" />
        <div className="min-w-0">
          <p className="font-heading text-base font-semibold text-foreground">
            Đã hủy đơn <span className="t-code">{result.orderCode}</span>
          </p>
          <p className="text-sm text-muted-foreground">
            {REFUND_SUMMARY[result.refundStatus]}
          </p>
        </div>
      </div>

      {cashDue > 0 && (
        <div
          data-testid="counter-cancel-cash-due"
          className="flex flex-col gap-1 cut-md bg-warning p-4 text-warning-foreground"
        >
          <p className="flex items-center gap-2 text-sm font-medium">
            <Banknote className="size-4" />
            Số tiền mặt cần trả lại khách
          </p>
          <p className="font-heading text-3xl font-bold tabular-nums sm:text-4xl">
            {formatCurrency(cashDue)}
          </p>
          <p className="text-xs">
            Đếm đủ tiền và trao cho khách trước khi khách rời quầy.
          </p>
        </div>
      )}

      {result.refunds.length > 0 && (
        <dl className="divide-y divide-border cut-sm bg-secondary px-4">
          {result.refunds.map((refund) => {
            const status = getRefundStatusInfo(refund.status);
            return (
              <div
                key={`${refund.method}-${refund.status}`}
                className="flex items-center justify-between gap-3 py-3"
              >
                <div className="flex min-w-0 flex-wrap items-center gap-2">
                  <dt className="text-sm font-medium text-foreground">
                    {getPaymentMethodLabel(refund.method)}
                  </dt>
                  <Badge variant={status.variant}>{status.label}</Badge>
                </div>
                <dd className="shrink-0 text-sm font-semibold tabular-nums text-foreground">
                  {formatCurrency(refund.amount)}
                </dd>
              </div>
            );
          })}
          <div className="flex items-center justify-between gap-3 py-3">
            <dt className="text-sm text-muted-foreground">Tổng tiền hoàn</dt>
            <dd
              data-testid="counter-cancel-total-refund"
              className="text-sm font-semibold tabular-nums text-foreground"
            >
              {formatCurrency(refundTotal)}
            </dd>
          </div>
        </dl>
      )}
    </section>
  );
};
