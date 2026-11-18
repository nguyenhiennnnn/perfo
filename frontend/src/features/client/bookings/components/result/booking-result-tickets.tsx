import { PerfoTicket } from "@/components/common/perfo-ticket";
import type { BookingDetailDto } from "@/features/client/bookings/types";

interface BookingResultTicketsProps {
  booking: BookingDetailDto;
  isTorn?: boolean;
}

export const BookingResultTickets = ({ booking, isTorn = false }: BookingResultTicketsProps) => {
  const seatPrices = new Map(booking.seats.map((seat) => [`${seat.seatRow}${seat.seatNumber}`, seat.price]));

  if (booking.tickets.length === 0) {
    return (
      <div className="flex w-full flex-col gap-4">
        <PerfoTicket
          movieTitle={booking.movie.title}
          startTime={booking.showtime.startTime}
          room={booking.showtime.roomName}
          seat={Array.from(seatPrices.keys()).join(", ")}
          price={booking.seats.reduce((sum, seat) => sum + seat.price, 0)}
          ticketCode={booking.bookingCode}
          qrValue={booking.bookingCode}
          isTorn={isTorn}
        />
      </div>
    );
  }

  return (
    <div className="flex w-full flex-col gap-4">
      {booking.tickets.map((ticket) => {
        const seatLabel = `${ticket.seatRow}${ticket.seatNumber}`;
        return (
          <PerfoTicket
            key={ticket.ticketId}
            movieTitle={booking.movie.title}
            startTime={booking.showtime.startTime}
            room={booking.showtime.roomName}
            seat={seatLabel}
            price={seatPrices.get(seatLabel) ?? 0}
            ticketCode={`${booking.bookingCode}-${seatLabel}`}
            qrValue={ticket.qrCodeImage}
            isCheckedIn={ticket.isCheckedIn}
            isTorn={isTorn}
          />
        );
      })}
    </div>
  );
};
