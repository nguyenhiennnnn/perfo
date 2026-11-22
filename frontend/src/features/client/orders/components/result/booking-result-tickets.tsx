import { PerfoTicket } from "@/components/common/perfo-ticket";
import type { OrderDetailDto } from "@/features/client/orders/types";
import { getOrderSeats, getOrderShowing, getTicketLines } from "../../utils/order-lines";

interface BookingResultTicketsProps {
  order: OrderDetailDto;
  isTorn?: boolean;
}

export const BookingResultTickets = ({ order, isTorn = false }: BookingResultTicketsProps) => {
  const showing = getOrderShowing(order);
  if (!showing) return null;

  const ticketLines = getTicketLines(order);
  const seatLabels = getOrderSeats(order).map((seat) => `${seat.seatRow}${seat.seatNumber}`);
  const hasQr = ticketLines.some((line) => line.ticket.qrCodeImage);

  if (!hasQr) {
    return (
      <div className="flex w-full flex-col gap-4">
        <PerfoTicket
          movieTitle={showing.movieTitle}
          startTime={showing.startTime}
          room={showing.roomName}
          seat={seatLabels.join(", ")}
          price={ticketLines.reduce((sum, line) => sum + line.lockedUnitPrice, 0)}
          ticketCode={order.orderCode}
          qrValue={order.orderCode}
          isTorn={isTorn}
        />
      </div>
    );
  }

  return (
    <div className="flex w-full flex-col gap-4">
      {ticketLines.map((line) => {
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
            isCheckedIn={ticket.isCheckedIn}
            isTorn={isTorn}
          />
        );
      })}
    </div>
  );
};
