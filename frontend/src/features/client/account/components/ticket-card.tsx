import { Link } from "react-router";
import { QrCode, Ticket as TicketIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { BookingStatusBadge } from "@/components/common/status-badge";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import type { OrderListItemDto } from "@/features/client/orders/types";
import { useCancelPolicy } from "@/features/client/orders/hooks/use-cancel-policy";
import { ROUTES } from "@/config/routes";

interface TicketCardProps {
  order: OrderListItemDto;
  onCancel?: () => void;
}

export const TicketCard = ({ order, onCancel }: TicketCardProps) => {
  const { canCancel } = useCancelPolicy(order.status, order.startTime, order.finalAmount);

  return (
    <div className="flex flex-col gap-4 bg-card p-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-center gap-3">
        <div className="flex size-12 shrink-0 items-center justify-center bg-muted">
          <TicketIcon className="size-5" />
        </div>
        <div>
          <p className="font-heading text-sm font-semibold text-foreground">{order.movieTitle}</p>
          <p className="mt-0.5 text-xs text-muted-foreground">{formatDateTime(order.startTime)}</p>
          <p className="mt-0.5 t-code text-xs text-muted-foreground">{order.orderCode}</p>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <div className="text-right">
          <BookingStatusBadge status={order.status} />
          <p className="mt-1 text-sm font-semibold text-primary-text tabular-nums">{formatCurrency(order.finalAmount)}</p>
        </div>
        <div className="flex flex-col gap-2">
          <Link
            to={ROUTES.account.ticketDetail(order.orderId)}
            className="inline-flex items-center justify-center gap-1.5 border border-border bg-muted px-2 py-0.5 text-xs font-medium"
          >
            <QrCode className="size-3.5" />
            Chi tiết & QR
          </Link>
          {canCancel && onCancel && (
            <Button variant="destructive" size="sm" onClick={onCancel}>
              Hủy vé
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}