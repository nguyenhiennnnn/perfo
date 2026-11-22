import { Gift, Receipt, ShoppingBag, Ticket, type LucideIcon } from "lucide-react";
import { ButtonLink } from "@/components/common/button-link";
import { BookingStatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { OrderStatus, OrderType } from "@/types/enum";
import { useCancelPolicy } from "../../hooks/use-cancel-policy";
import type { OrderListItemDto } from "../../types";
import { getOrderTypeLabel } from "../../utils/order-labels";

const TYPE_ICONS: Record<string, LucideIcon> = {
  [OrderType.Ticket]: Ticket,
  [OrderType.Combo]: ShoppingBag,
  [OrderType.GiftCard]: Gift,
};

interface OrderCardProps {
  order: OrderListItemDto;
  onCancel?: () => void;
}

export const OrderCard = ({ order, onCancel }: OrderCardProps) => {
  const { canCancel } = useCancelPolicy(order.status, order.startTime, order.finalAmount);
  const Icon = TYPE_ICONS[order.type] ?? Receipt;
  const typeLabel = getOrderTypeLabel(order.type);
  const hasShowtime = Boolean(order.movieTitle && order.startTime);
  const isPending = order.status === OrderStatus.Pending;

  return (
    <article
      aria-label={`Đơn ${order.orderCode}`}
      className="flex flex-col gap-4 cut-md bg-card p-4 sm:flex-row sm:items-center sm:justify-between"
    >
      <div className="flex min-w-0 items-start gap-3">
        <div className="flex size-12 shrink-0 items-center justify-center cut-xs bg-secondary text-muted-foreground">
          <Icon className="size-5" />
        </div>
        <div className="min-w-0">
          <p className="text-xs text-muted-foreground">{typeLabel}</p>
          <h2 className="mt-0.5 line-clamp-2 font-heading text-sm font-semibold text-foreground">
            {hasShowtime ? order.movieTitle : typeLabel}
          </h2>
          {hasShowtime && (
            <p className="mt-0.5 text-xs text-muted-foreground">
              {formatDateTime(order.startTime)}, {order.ticketCount} vé
            </p>
          )}
          <p className="mt-0.5 t-code text-xs text-muted-foreground">{order.orderCode}</p>
          <p className="mt-0.5 text-xs text-muted-foreground">Đặt lúc {formatDateTime(order.createdAt)}</p>
          {isPending && order.expiresAt && (
            <p className="mt-0.5 text-xs font-medium text-warning-text">
              Giữ chỗ đến {formatDateTime(order.expiresAt)}
            </p>
          )}
        </div>
      </div>

      <div className="flex items-center justify-between gap-4 sm:justify-end">
        <div className="sm:text-right">
          <BookingStatusBadge status={order.status} />
          <p className="mt-1 text-sm font-semibold text-primary-text tabular-nums">
            {formatCurrency(order.finalAmount)}
          </p>
        </div>
        <div className="flex flex-col gap-2">
          <ButtonLink to={ROUTES.account.ticketDetail(order.orderId)} variant="secondary" size="sm">
            Xem chi tiết
          </ButtonLink>
          {canCancel && onCancel && (
            <Button variant="destructive" size="sm" onClick={onCancel}>
              Hủy vé
            </Button>
          )}
        </div>
      </div>
    </article>
  );
};
