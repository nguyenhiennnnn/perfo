import { BookingChannelBadge, BookingStatusBadge } from "@/components/common/status-badge";
import { CopyButton } from "@/components/common/copy-button";
import { formatDateTime } from "@/lib/formatters";
import type { OrderDetailDto } from "../../types";
import { getOrderTypeLabel } from "../../utils/order-labels";

interface OrderDetailHeaderProps {
  order: OrderDetailDto;
}

export const OrderDetailHeader = ({ order }: OrderDetailHeaderProps) => (
  <div className="flex flex-wrap items-start justify-between gap-3 cut-md bg-card p-4">
    <div>
      <div className="flex items-center gap-2">
        <h1 className="t-code text-lg text-foreground">{order.orderCode}</h1>
        <CopyButton value={order.orderCode} label="mã đơn" />
      </div>
      <p className="mt-1 text-sm text-muted-foreground">{getOrderTypeLabel(order.type)}</p>
      <p className="mt-0.5 text-xs text-muted-foreground">Đặt lúc {formatDateTime(order.createdAt)}</p>
      {order.customerName && (
        <p className="mt-0.5 text-xs text-muted-foreground">
          Người đặt: {order.customerName}
          {order.customerPhone ? `, ${order.customerPhone}` : ""}
        </p>
      )}
    </div>
    <div className="flex flex-wrap items-center gap-2">
      <BookingChannelBadge channel={order.channel} />
      <span data-testid="ticket-detail-status">
        <BookingStatusBadge status={order.status} />
      </span>
    </div>
  </div>
);
