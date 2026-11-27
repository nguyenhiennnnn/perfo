import { CheckCircle2, Gift, ScanLine } from "lucide-react";

import { BookingStatusBadge } from "@/components/common/status-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { FORM_PROCESSING_LABEL } from "@/config/default-constants";
import { formatDateTime } from "@/lib/formatters";
import { OrderStatus } from "@/types/enum";

import type { EventGiftHandoverFlow } from "../hooks/use-event-gift-handover-flow";
import type { EventGiftHandoverTicketDto } from "../types";

interface EventGiftTicketCardProps {
  flow: EventGiftHandoverFlow;
  ticket: EventGiftHandoverTicketDto;
}

export const EventGiftTicketCard = ({ flow, ticket }: EventGiftTicketCardProps) => {
  const isConfirmed = ticket.orderStatus === OrderStatus.Confirmed;
  const canConfirm = isConfirmed && ticket.totalRemaining > 0 && !flow.isSubmitting && !flow.isLooking;

  return (
    <Card className="gap-4 p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="t-code text-lg font-bold text-foreground">{ticket.orderCode}</p>
          <p className="text-sm text-muted-foreground">{ticket.customerName || "Khách lẻ"}</p>
        </div>
        <BookingStatusBadge status={ticket.orderStatus} />
      </div>

      <Separator />

      {ticket.totalRemaining <= 0 ? (
        <div className="flex flex-col items-center gap-2 py-4 text-center">
          <CheckCircle2 className="size-8 text-success" />
          <p className="font-heading text-base font-semibold text-foreground">
            {flow.lastHanded
              ? `Đã trao ${flow.lastHanded} quà, vé đã nhận đủ quà`
              : "Vé này đã nhận đủ quà"}
          </p>
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          {!isConfirmed && (
            <p role="alert" className="bg-warning/10 px-3 py-2 text-sm text-warning-text">
              Đơn này chưa ở trạng thái đã thanh toán nên chưa thể trao quà.
            </p>
          )}
          <ul className="flex flex-col gap-2">
            {ticket.gifts.map((gift) => (
              <li
                key={gift.reservationId}
                className="flex flex-wrap items-center justify-between gap-3 bg-muted/40 p-3"
              >
                <div className="min-w-0">
                  <p className="wrap-break-word text-sm font-medium text-foreground">{gift.name}</p>
                  <p className="text-xs tabular-nums text-muted-foreground">
                    Đã trao {gift.given}/{gift.quantity} {gift.unit} · còn {gift.remaining}
                  </p>
                </div>
                {gift.remaining <= 0 && <Badge variant="success">Đã đủ</Badge>}
              </li>
            ))}
          </ul>
          {isConfirmed && (
            <Button onClick={flow.confirmHandOver} disabled={!canConfirm}>
              <Gift className="size-4" />
              <span>
                {flow.isSubmitting
                  ? FORM_PROCESSING_LABEL
                  : `Xác nhận trao ${ticket.totalRemaining} quà`}
              </span>
            </Button>
          )}
        </div>
      )}

      <Separator />

      <div className="flex flex-col gap-2">
        <h3 className="text-sm font-medium text-foreground">Lịch sử trao quà</h3>
        {ticket.history.length === 0 ? (
          <p className="text-sm text-muted-foreground">Chưa trao quà nào cho vé này.</p>
        ) : (
          <ul className="flex flex-col gap-1 text-sm">
            {ticket.history.map((entry) => (
              <li key={entry.handoverId} className="flex flex-wrap justify-between gap-2">
                <span className="text-foreground">
                  {entry.giftName} × {entry.quantity}
                </span>
                <span className="text-xs text-muted-foreground">
                  {entry.handedByName} · {formatDateTime(entry.handedAt)}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>

      <Button variant="outline" onClick={flow.reset} disabled={flow.isSubmitting}>
        <ScanLine className="size-4" />
        <span>Quét vé khác</span>
      </Button>
    </Card>
  );
};
