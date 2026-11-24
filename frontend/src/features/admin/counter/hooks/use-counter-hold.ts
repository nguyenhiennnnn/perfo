import { useState } from "react";
import { toast } from "@/components/ui/toast";
import { useRealtimeSeatMap } from "@/hooks/use-realtime-seat-map";
import { CancelReason } from "@/types/enum";
import { useCounterHoldOrder, useCounterCancelOrder } from "./use-counter-orders";
import type { CounterHoldRequest, CounterOrderResponse } from "../types";
import type { OrderPromotionResponse } from "@/features/client/orders/types";
import type { ShowtimeSeatItemDto } from "@/features/client/showtimes/types";
import type { CounterLeaveCancel } from "./use-counter-leave-cancel";

interface UseCounterHoldFlowParams {
  showtimeId: number | null;
  initialSeats: ShowtimeSeatItemDto[] | undefined;
  onHoldExpired: () => void;
  onSeatsBlocked: (seatIds: number[]) => void;
  leave: CounterLeaveCancel;
}

export function useCounterHoldFlow({
  showtimeId,
  initialSeats,
  onHoldExpired,
  onSeatsBlocked,
  leave,
}: UseCounterHoldFlowParams) {
  const [order, setOrder] = useState<CounterOrderResponse | null>(null);

  const holdOrder = useCounterHoldOrder();
  const cancelOrder = useCounterCancelOrder();

  const seatStatusMap = useRealtimeSeatMap({
    showtimeId,
    initialSeats,
    ownOrderId: order?.orderId ?? null,
    onHoldExpired: () => {
      toast.add({ title: "Thời gian giữ ghế đã hết hạn.", type: "warning" });
      onHoldExpired();
    },
    onSeatsBlocked,
  });

  const cancelHold = async (
    reason: CancelReason = CancelReason.Other,
  ) => {
    if (!order) return true;
    try {
      await cancelOrder.mutateAsync({ orderId: order.orderId, reason });
    } catch {
      return false;
    }
    setOrder(null);
    return true;
  };

  const holdSeats = async (request: CounterHoldRequest, onHeld: () => void) => {
    const res = await holdOrder.mutateAsync(request);
    if (leave.hasLeft()) {
      await leave.cancelQuietly(res.data.orderId);
      return;
    }
    setOrder(res.data);
    onHeld();
  };

  const clearHold = () => setOrder(null);

  const updatePricing = (pricing: OrderPromotionResponse) =>
    setOrder((current) =>
      current && current.orderId === pricing.orderId
        ? {
            ...current,
            totalAmount: pricing.totalAmount,
            discountAmount: pricing.discountAmount,
            finalAmount: pricing.finalAmount,
            expiresAt: pricing.expiresAt,
          }
        : current,
    );

  return {
    order,
    seatStatusMap,
    holdSeats,
    cancelHold,
    clearHold,
    updatePricing,
    isHolding: holdOrder.isPending,
    isCancelling: cancelOrder.isPending,
  };
}
