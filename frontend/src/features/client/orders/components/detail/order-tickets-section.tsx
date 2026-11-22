import { PerfoTicket } from "@/components/common/perfo-ticket";
import { DownloadTicketsPdfButton } from "@/components/common/download-tickets-pdf-button";
import { ENDPOINTS } from "@/api/endpoints";
import { OrderStatus } from "@/types/enum";
import type { OrderDetailDto } from "../../types";
import { getActiveTicketLines } from "../../utils/order-lines";

interface OrderTicketCardsProps {
  order: OrderDetailDto;
  isTorn?: boolean;
}

export const OrderTicketCards = ({ order, isTorn = false }: OrderTicketCardsProps) => {
  const lines = getActiveTicketLines(order);
  if (lines.length === 0) return null;

  return (
    <div className="flex w-full flex-col gap-4">
      {lines.map((line) => {
        const { ticket } = line;
        const seatLabel = `${ticket.seatRow}${ticket.seatNumber}`;
        return (
          <PerfoTicket
            key={line.orderLineId}
            movieTitle={ticket.movieTitle}
            startTime={ticket.startTime}
            room={ticket.roomName}
            seat={seatLabel}
            price={line.lockedUnitPrice}
            ticketCode={`${order.orderCode}-${seatLabel}`}
            qrValue={ticket.qrCodeImage ?? order.orderCode}
            ticketType={line.description?.trim() || null}
            isCheckedIn={ticket.isCheckedIn}
            checkedInAt={ticket.checkedInAt}
            isTorn={isTorn}
          />
        );
      })}
    </div>
  );
};

interface OrderTicketsSectionProps {
  order: OrderDetailDto;
}

export const OrderTicketsSection = ({ order }: OrderTicketsSectionProps) => {
  if (order.status !== OrderStatus.Confirmed || getActiveTicketLines(order).length === 0) return null;

  return (
    <section aria-labelledby="order-tickets-title">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 id="order-tickets-title" className="t-h2 text-foreground">
          Vé điện tử & Mã QR
        </h2>
        <DownloadTicketsPdfButton
          endpoint={ENDPOINTS.orders.ticketsPdf(order.orderId)}
          fallbackFilename={`tickets-${order.orderCode}.pdf`}
        />
      </div>
      <div className="mt-3">
        <OrderTicketCards order={order} />
      </div>
    </section>
  );
};
