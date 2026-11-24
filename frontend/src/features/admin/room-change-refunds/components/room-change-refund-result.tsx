import { CheckCircle2 } from "lucide-react";

import { ButtonLink } from "@/components/common/button-link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import {
  getPaymentMethodLabel,
  getRefundStatusInfo,
} from "@/features/admin/orders/utils/order-cancel";
import { formatCurrency } from "@/lib/formatters";

import type { RoomChangeRefundConfirmResultDto } from "../types";

interface RoomChangeRefundResultProps {
  result: RoomChangeRefundConfirmResultDto;
  onClose: () => void;
}

export const RoomChangeRefundResult = ({ result, onClose }: RoomChangeRefundResultProps) => (
  <section className="flex flex-col gap-4">
    <div className="flex items-start gap-3">
      <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-success" />
      <div className="min-w-0">
        <p className="font-heading text-base font-semibold text-foreground">
          Đã xác nhận hoàn chênh lệch đơn <span className="t-code">{result.orderCode}</span>
        </p>
        <p className="text-sm text-muted-foreground">
          Số tiền hoàn {formatCurrency(result.amount)}, chia theo các phương thức đã thanh toán.
        </p>
      </div>
    </div>

    {result.transactions.length > 0 && (
      <dl className="divide-y divide-border cut-sm bg-secondary px-4">
        {result.transactions.map((transaction) => {
          const status = getRefundStatusInfo(transaction.status);
          return (
            <div
              key={transaction.refundTransactionId}
              className="flex items-center justify-between gap-3 py-3"
            >
              <div className="flex min-w-0 flex-wrap items-center gap-2">
                <dt className="text-sm font-medium text-foreground">
                  {getPaymentMethodLabel(transaction.method)}
                </dt>
                <Badge variant={status.variant}>{status.label}</Badge>
              </div>
              <dd className="shrink-0 text-sm font-semibold tabular-nums text-foreground">
                {formatCurrency(transaction.amount)}
              </dd>
            </div>
          );
        })}
        <div className="flex items-center justify-between gap-3 py-3">
          <dt className="text-sm text-muted-foreground">Tổng tiền hoàn</dt>
          <dd className="text-sm font-semibold tabular-nums text-foreground">
            {formatCurrency(
              result.transactions.reduce((sum, transaction) => sum + transaction.amount, 0),
            )}
          </dd>
        </div>
      </dl>
    )}

    <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
      <ButtonLink
        variant="outline"
        to={ROUTES.admin.orderDetail(result.orderId)}
        onClick={onClose}
      >
        Xem đơn hàng
      </ButtonLink>
      <Button onClick={onClose}>Đóng</Button>
    </div>
  </section>
);
