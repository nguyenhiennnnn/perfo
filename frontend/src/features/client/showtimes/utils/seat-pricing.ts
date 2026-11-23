import type {
  ShowtimeSeatItemDto,
  ShowtimeTicketTypeDto,
} from "@/features/client/showtimes/types";
import type { SeatTicketSelectionRequest } from "@/features/client/orders/types";

export const getSeatPrice = (
  seat: ShowtimeSeatItemDto,
  ticketTypeId: number | null | undefined,
): number => {
  if (ticketTypeId == null) return seat.price;
  return (
    seat.ticketPrices?.find((p) => p.ticketTypeId === ticketTypeId)?.price ??
    seat.price
  );
};

export const getDefaultTicketTypeId = (
  ticketTypes: ShowtimeTicketTypeDto[] | null | undefined,
): number | null => {
  const usable = (ticketTypes ?? []).filter(
    (t) => t.isEligible && !t.requiresCardVerification,
  );
  return (
    usable.find((t) => t.priceMultiplier === 1) ?? usable[0]
  )?.ticketTypeId ?? null;
};

export interface PricedSelectedSeat extends ShowtimeSeatItemDto {
  ticketTypeId: number | null;
  ticketType: ShowtimeTicketTypeDto | null;
  unitPrice: number;
}

export const priceSelectedSeats = (
  seats: ShowtimeSeatItemDto[],
  ticketTypes: ShowtimeTicketTypeDto[] | null | undefined,
  chosen: Record<number, number | null>,
  defaultTicketTypeId: number | null,
): PricedSelectedSeat[] =>
  seats.map((seat) => {
    const ticketTypeId =
      seat.showtimeSeatId in chosen
        ? chosen[seat.showtimeSeatId]
        : defaultTicketTypeId;
    return {
      ...seat,
      ticketTypeId,
      ticketType: ticketTypes?.find((t) => t.ticketTypeId === ticketTypeId) ?? null,
      unitPrice: getSeatPrice(seat, ticketTypeId),
    };
  });

export const toTicketSelections = (
  seats: PricedSelectedSeat[],
): SeatTicketSelectionRequest[] =>
  seats.map((seat) => ({
    showtimeSeatId: seat.showtimeSeatId,
    ticketTypeId: seat.ticketTypeId,
    expectedPrice: seat.unitPrice,
  }));

export const sumSeatPrices = (seats: { unitPrice: number }[]): number =>
  seats.reduce((sum, seat) => sum + seat.unitPrice, 0);
