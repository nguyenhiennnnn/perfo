import { useEffect, useReducer } from "react";
import { CancelReason } from "@/types/enum";
import {
  counterFlowReducer,
  initialCounterStep,
  type CounterStep,
} from "./counter-flow-reducer";
import { useSeatTicketTypes } from "@/features/client/showtimes/hooks/use-seat-ticket-types";
import { usePriceChangeConfirm } from "@/hooks/use-price-change-confirm";
import { getPriceChangeRequired } from "@/lib/error";
import { useCounterSelection } from "./use-counter-selection";
import { useCounterHoldFlow } from "./use-counter-hold";
import { useCounterCheckoutFlow } from "./use-counter-checkout";
import { useCounterLeaveCancel } from "./use-counter-leave-cancel";
import type { CounterCheckoutRequest } from "../types";

export type { CounterStep };

export function useCounterFlow() {
  const [step, dispatch] = useReducer(counterFlowReducer, initialCounterStep);

  const leave = useCounterLeaveCancel({
    onRestoredAfterCancel: () => resetLocalFlow(),
  });
  const selection = useCounterSelection();
  const seatTicketTypes = useSeatTicketTypes(
    selection.showtimeSeats.data,
    selection.selectedSeatIds,
  );
  const confirmPriceChange = usePriceChangeConfirm();
  const selectedSeatsInfo = seatTicketTypes.pricedSeats.map((seat) => ({
    ...seat,
    price: seat.unitPrice,
  }));
  const hold = useCounterHoldFlow({
    showtimeId: selection.showtimeId,
    initialSeats: selection.showtimeSeats.data?.seats,
    onHoldExpired: () => {
      if (step !== "done") resetLocalFlow();
    },
    leave,
  });
  const checkout = useCounterCheckoutFlow(hold.order, step === "done");

  const hasPartialPayment = step !== "done" && checkout.totalPaid > 0;

  const cancellableOrderId =
    step !== "done" && !checkout.isPaying && !hasPartialPayment
      ? (hold.order?.orderId ?? null)
      : null;
  const { setCancellableOrderId } = leave;
  useEffect(() => {
    setCancellableOrderId(cancellableOrderId);
  }, [setCancellableOrderId, cancellableOrderId]);

  const setStep = (next: CounterStep) =>
    dispatch({ type: "SET_STEP", step: next });

  const pickShowtime = async (id: number) => {
    if (id !== selection.showtimeId && !(await hold.cancelHold())) return;
    selection.selectShowtime(id);
    seatTicketTypes.reset();
    dispatch({ type: "SHOWTIME_PICKED" });
  };

  const toggleSeat = (showtimeSeatId: number) =>
    selection.toggleSeat(showtimeSeatId, hold.seatStatusMap);

  const holdSeats = async () => {
    const onHeld = () => dispatch({ type: "HOLD_SUCCEEDED" });
    const request = selection.buildHoldRequest({ tickets: seatTicketTypes.tickets });
    if (!request) return;
    try {
      await hold.holdSeats(request, onHeld);
    } catch (error) {
      const change = getPriceChangeRequired(error);
      if (!change) return;
      const accepted = await confirmPriceChange(
        change,
        seatTicketTypes.pricedSeats,
        seatTicketTypes.seatsTotal,
      );
      if (!accepted) {
        void selection.showtimeSeats.refetch();
        return;
      }
      try {
        await hold.holdSeats({ ...request, acceptPriceChange: true }, onHeld);
      } catch {
        return;
      }
    }
  };

  const pay = async (request: CounterCheckoutRequest) => {
    await checkout.pay(request, () => dispatch({ type: "CHECKOUT_SUCCEEDED" }));
  };

  const backToSeats = async () => {
    if (hasPartialPayment) return;
    if (!(await hold.cancelHold())) return;
    dispatch({ type: "BACK_TO_SEATS" });
  };

  const resetLocalFlow = () => {
    dispatch({ type: "RESET" });
    selection.resetSelection();
    seatTicketTypes.reset();
    hold.clearHold();
    checkout.clearCheckout();
  };

  const cancelOrder = async () => {
    if (hasPartialPayment) return;
    if (!(await hold.cancelHold(CancelReason.CustomerChangedMind))) return;
    resetLocalFlow();
  };

  const resetFlow = async () => {
    if (hasPartialPayment) return;
    if (step !== "done" && !(await hold.cancelHold())) return;
    resetLocalFlow();
  };

  return {
    step,
    setStep,
    selectedDate: selection.selectedDate,
    setSelectedDate: selection.setSelectedDate,
    showtimeId: selection.showtimeId,
    showtimeSeats: selection.showtimeSeats,
    seatStatusMap: hold.seatStatusMap,
    selectedSeatIds: selection.selectedSeatIds,
    selectedSeatsInfo,
    seatTicketTypes,
    toggleSeat,
    comboQuantities: selection.comboQuantities,
    setComboQuantity: selection.setComboQuantity,
    customerName: selection.customerName,
    setCustomerName: selection.setCustomerName,
    customerPhone: selection.customerPhone,
    setCustomerPhone: selection.setCustomerPhone,
    userId: selection.userId,
    setUserId: selection.setUserId,
    canHold: selection.canHold,
    holdSeats,
    isHolding: hold.isHolding,
    order: hold.order,
    pay,
    isPaying: checkout.isPaying,
    isCancelling: hold.isCancelling,
    payments: checkout.payments,
    totalPaid: checkout.totalPaid,
    outstandingAmount: checkout.outstandingAmount,
    totalChange: checkout.totalChange,
    hasPartialPayment,
    orderDetail: checkout.orderDetail,
    pickShowtime,
    backToSeats,
    cancelOrder,
    resetFlow,
  };
}

export type CounterFlow = ReturnType<typeof useCounterFlow>;
