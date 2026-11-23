import { useMemo, useState } from "react";
import { getBusinessToday } from "@/lib/business-time";
import { ShowtimeSeatStatus } from "@/types/enum";
import { toggleSeatSelection } from "@/components/common/seat-map/seat-map-utils";
import { useShowtimeSeats } from "@/features/client/showtimes/hooks/use-showtimes";
import type { CounterHoldRequest } from "../types";

export function useCounterSelection() {
  const [selectedDate, setSelectedDate] = useState(() => getBusinessToday());
  const [showtimeId, setShowtimeId] = useState<number | null>(null);
  const [selectedSeatIds, setSelectedSeatIds] = useState<Set<number>>(
    new Set(),
  );
  const [comboQuantities, setComboQuantities] = useState<
    Record<number, number>
  >({});
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [userId, setUserId] = useState<number | null>(null);

  const showtimeSeats = useShowtimeSeats(showtimeId ?? 0);

  const combosPayload = useMemo(
    () =>
      Object.entries(comboQuantities)
        .filter(([, qty]) => qty > 0)
        .map(([comboId, quantity]) => ({ comboId: Number(comboId), quantity })),
    [comboQuantities],
  );

  const selectedSeatsInfo = useMemo(
    () =>
      (showtimeSeats.data?.seats ?? []).filter((s) =>
        selectedSeatIds.has(s.showtimeSeatId),
      ),
    [showtimeSeats.data, selectedSeatIds],
  );

  const selectShowtime = (id: number) => {
    setShowtimeId(id);
    setSelectedSeatIds(new Set());
  };

  const toggleSeat = (
    showtimeSeatId: number,
    statusMap: Map<number, ShowtimeSeatStatus>,
  ) => {
    const seats = showtimeSeats.data?.seats ?? [];
    setSelectedSeatIds((prev) =>
      toggleSeatSelection(prev, seats, statusMap, showtimeSeatId),
    );
  };

  const setComboQuantity = (comboId: number, quantity: number) => {
    setComboQuantities((prev) => ({ ...prev, [comboId]: quantity }));
  };

  const canHold =
    showtimeId != null &&
    selectedSeatIds.size > 0 &&
    (userId != null || customerName.trim().length > 0);

  const buildHoldRequest = (
    extra?: Pick<CounterHoldRequest, "tickets" | "acceptPriceChange">,
  ): CounterHoldRequest | null => {
    if (!showtimeId) return null;
    return {
      showtimeId,
      seatIds: Array.from(selectedSeatIds),
      combos: combosPayload.length > 0 ? combosPayload : undefined,
      userId: userId ?? undefined,
      customerName: customerName.trim(),
      customerPhone: customerPhone.trim() || undefined,
      ...extra,
    };
  };

  const resetSelection = () => {
    setShowtimeId(null);
    setSelectedSeatIds(new Set());
    setComboQuantities({});
    setCustomerName("");
    setCustomerPhone("");
    setUserId(null);
  };

  return {
    selectedDate,
    setSelectedDate,
    showtimeId,
    showtimeSeats,
    selectShowtime,
    selectedSeatIds,
    selectedSeatsInfo,
    toggleSeat,
    comboQuantities,
    setComboQuantity,
    combosPayload,
    customerName,
    setCustomerName,
    customerPhone,
    setCustomerPhone,
    userId,
    setUserId,
    canHold,
    buildHoldRequest,
    resetSelection,
  };
}
