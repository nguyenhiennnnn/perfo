import { useCallback, useMemo, useState } from "react";
import type {
  ShowtimeSeatsResponseDto,
  ShowtimeSeatItemDto,
} from "@/features/client/showtimes/types";
import {
  getDefaultTicketTypeId,
  priceSelectedSeats,
  sumSeatPrices,
  toTicketSelections,
} from "../utils/seat-pricing";

export function useSeatTicketTypes(
  data: ShowtimeSeatsResponseDto | undefined,
  selectedSeatIds: Set<number>,
) {
  const [chosen, setChosen] = useState<Record<number, number | null>>({});

  const ticketTypes = data?.ticketTypes ?? [];
  const defaultTicketTypeId = useMemo(
    () => getDefaultTicketTypeId(data?.ticketTypes),
    [data?.ticketTypes],
  );

  const selectedSeats: ShowtimeSeatItemDto[] = useMemo(
    () => (data?.seats ?? []).filter((s) => selectedSeatIds.has(s.showtimeSeatId)),
    [data?.seats, selectedSeatIds],
  );

  const pricedSeats = useMemo(
    () => priceSelectedSeats(selectedSeats, data?.ticketTypes, chosen, defaultTicketTypeId),
    [selectedSeats, data?.ticketTypes, chosen, defaultTicketTypeId],
  );

  const setTicketType = useCallback((showtimeSeatId: number, ticketTypeId: number | null) => {
    setChosen((prev) => ({ ...prev, [showtimeSeatId]: ticketTypeId }));
  }, []);

  const setAllTicketTypes = useCallback(
    (ticketTypeId: number | null) => {
      setChosen((prev) => {
        const next = { ...prev };
        selectedSeats.forEach((s) => {
          next[s.showtimeSeatId] = ticketTypeId;
        });
        return next;
      });
    },
    [selectedSeats],
  );

  const reset = useCallback(() => setChosen({}), []);

  return {
    ticketTypes,
    pricedSeats,
    seatsTotal: sumSeatPrices(pricedSeats),
    tickets: toTicketSelections(pricedSeats),
    cardRequiredSeats: pricedSeats.filter((s) => s.ticketType?.requiresCardVerification),
    setTicketType,
    setAllTicketTypes,
    reset,
  };
}

export type SeatTicketTypes = ReturnType<typeof useSeatTicketTypes>;
