import { XCircle } from "lucide-react";
import { formatDateTime } from "@/lib/formatters";
import { OrderStatus } from "@/types/enum";
import type { OrderDetailDto } from "../../types";
import { getCancelledByLabel, getCancelReasonLabel } from "../../utils/order-labels";

interface OrderCancelSectionProps {
  order: OrderDetailDto;
}

export const OrderCancelSection = ({ order }: OrderCancelSectionProps) => {
  if (order.status !== OrderStatus.Cancelled) return null;

  return (
    <section aria-labelledby="order-cancel-title" className="cut-md bg-card p-4">
      <h2 id="order-cancel-title" className="t-h2 flex items-center gap-2 text-foreground">
        <XCircle className="size-5 text-destructive" />
        Thông tin hủy đơn
      </h2>
      <dl className="mt-3 grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
        <dt className="text-muted-foreground">Lý do</dt>
        <dd data-testid="order-cancel-reason" className="font-medium text-foreground">
          {order.cancelReason ? getCancelReasonLabel(order.cancelReason) : "Không ghi nhận"}
        </dd>
        <dt className="text-muted-foreground">Người hủy</dt>
        <dd className="font-medium text-foreground">
          {order.cancelledBy ? getCancelledByLabel(order.cancelledBy) : "Không ghi nhận"}
        </dd>
        {order.cancelledAt && (
          <>
            <dt className="text-muted-foreground">Thời điểm</dt>
            <dd className="font-medium text-foreground">
              {formatDateTime(order.cancelledAt)}
            </dd>
          </>
        )}
      </dl>
    </section>
  );
};
