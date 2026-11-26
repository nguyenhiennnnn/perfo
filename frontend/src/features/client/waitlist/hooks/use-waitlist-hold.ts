import { useMemo } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { queryKeys } from "@/api/query-keys";
import { ROUTES } from "@/config/routes";
import { useHoldSeats } from "@/features/client/orders/hooks/use-orders";
import { useShowtimeSeats } from "@/features/client/showtimes/hooks/use-showtimes";
import { useSeatTicketTypes } from "@/features/client/showtimes/hooks/use-seat-ticket-types";
import { usePriceChangeConfirm } from "@/hooks/use-price-change-confirm";
import { getPriceChangeRequired } from "@/lib/error";
import { useAuthUser } from "@/stores/auth-store";

export function useWaitlistHold(showtimeId: number, reservedSeatIds: number[]) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const authUser = useAuthUser();
  const seats = useShowtimeSeats(showtimeId);
  const holdMutation = useHoldSeats();
  const confirmPriceChange = usePriceChangeConfirm();

  const selectedIds = useMemo(() => new Set(reservedSeatIds), [reservedSeatIds]);
  const ticketTypes = useSeatTicketTypes(seats.data, selectedIds);

  const seatLabels = useMemo(
    () =>
      (seats.data?.seats ?? [])
        .filter((seat) => selectedIds.has(seat.showtimeSeatId))
        .sort((a, b) => a.seatRow.localeCompare(b.seatRow) || a.seatNumber - b.seatNumber)
        .map((seat) => `${seat.seatRow}${seat.seatNumber}`),
    [seats.data?.seats, selectedIds],
  );

  const submit = async (acceptPriceChange: boolean) => {
    const res = await holdMutation.mutateAsync({
      showtimeId,
      seatIds: reservedSeatIds,
      customerName: authUser?.fullName ?? "",
      tickets: ticketTypes.tickets,
      acceptPriceChange,
    });
    navigate(`${ROUTES.booking.detail(showtimeId)}?resume=${res.data.orderId}`);
  };

  const refreshEntry = () =>
    queryClient.invalidateQueries({ queryKey: queryKeys.waitlist.mine(showtimeId) });

  const holdSeats = async () => {
    try {
      await submit(false);
    } catch (error) {
      const change = getPriceChangeRequired(error);
      if (!change) {
        void refreshEntry();
        return;
      }
      const accepted = await confirmPriceChange(
        change,
        ticketTypes.pricedSeats,
        ticketTypes.seatsTotal,
      );
      if (!accepted) return;
      try {
        await submit(true);
      } catch {
        void refreshEntry();
      }
    }
  };

  return {
    seatLabels,
    isReady: !!seats.data && reservedSeatIds.length > 0,
    isHolding: holdMutation.isPending,
    holdSeats,
  };
}
