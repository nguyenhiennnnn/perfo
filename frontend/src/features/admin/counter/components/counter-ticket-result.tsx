import { CheckCircle2, PlusCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { UseQueryResult } from "@tanstack/react-query";
import { PerfoTicket } from "@/components/common/perfo-ticket";
import { QueryState } from "@/components/common/query-state";
import { TicketSkeleton } from "@/components/common/skeleton-shapes";
import { formatCurrency } from "@/lib/formatters";
import type {
  AdminBookingDetailDto,
  CounterCheckoutResponse,
} from "@/features/admin/bookings/types";

interface CounterTicketResultProps {
  bookingQuery: UseQueryResult<AdminBookingDetailDto>;
  checkout: CounterCheckoutResponse;
  onNewSale: () => void;
}

export const CounterTicketResult = ({ bookingQuery, checkout, onNewSale }: CounterTicketResultProps) => {
  const booking = bookingQuery.data;
  const seatPrices = new Map((booking?.seats ?? []).map((seat) => [`${seat.seatRow}${seat.seatNumber}`, seat.price]));

  return (
    <div className="flex flex-col items-center gap-6 cut-lg bg-card p-6 text-center" aria-busy={bookingQuery.isLoading || undefined}>
      <CheckCircle2 className="size-10 text-success" />
      <div>
        <p className="font-heading text-lg font-semibold text-foreground">Thanh toán thành công</p>
        <p className="text-sm text-muted-foreground">Tiền thừa trả khách: {formatCurrency(checkout.change)}</p>
      </div>

      <QueryState
        query={bookingQuery}
        skeleton={<TicketSkeleton className="w-full max-w-3xl" />}
        errorTitle="Không tải được vé vừa bán"
        errorClassName="w-full max-w-3xl"
      >
        {booking && booking.tickets && booking.tickets.length > 0 ? (
        <div className="flex w-full max-w-3xl flex-col gap-4">
          {booking.tickets.map((ticket) => (
            <PerfoTicket
              key={ticket.ticketId}
              movieTitle={booking.movie?.title ?? ""}
              startTime={booking.showtime.startTime}
              room={booking.showtime.roomName}
              seat={`${ticket.seatRow}${ticket.seatNumber}`}
              price={seatPrices.get(`${ticket.seatRow}${ticket.seatNumber}`) ?? 0}
              ticketCode={`${booking.bookingCode}-${ticket.seatRow}${ticket.seatNumber}`}
              qrValue={ticket.qrCodeImage}
              isCheckedIn={ticket.isCheckedIn}
            />
          ))}
        </div>
        ) : booking ? (
          <PerfoTicket
            movieTitle={booking.movie?.title ?? ""}
            startTime={booking.showtime.startTime}
            room={booking.showtime.roomName}
            seat={Array.from(seatPrices.keys()).join(", ")}
            price={booking.seats.reduce((sum, seat) => sum + seat.price, 0)}
            ticketCode={booking.bookingCode}
            qrValue={booking.bookingCode}
          />
        ) : null}
      </QueryState>

      <Button onClick={onNewSale}>
        <PlusCircle className="size-4" />
        <span>Bán vé mới</span>
      </Button>
    </div>
  );
};
