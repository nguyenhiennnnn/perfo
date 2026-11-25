import { useState } from "react";
import { useNavigate } from "react-router";
import { ROUTES } from "@/config/routes";
import { getServerNow } from "@/lib/server-clock";
import { CancelReason, OrderStatus, OrderType, PaymentMethod } from "@/types/enum";
import { useApplyPayment, useCancelOrder, useOrderDetail } from "./use-orders";

export type SupplementInvalidReason = "load-error" | "not-pending" | "expired";

export function useSupplementPaymentFlow(orderId: number) {
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.VNPay);
  const navigate = useNavigate();

  const orderQuery = useOrderDetail(orderId, { alwaysRefetchOnMount: true });
  const applyPayment = useApplyPayment(orderId);
  const cancelOrder = useCancelOrder();

  const order = orderQuery.data;

  let invalidReason: SupplementInvalidReason | null = null;
  if (orderQuery.isError) invalidReason = "load-error";
  else if (order) {
    if (order.type !== OrderType.Supplement || order.status !== OrderStatus.Pending) {
      invalidReason = "not-pending";
    } else if (!order.expiresAt || new Date(order.expiresAt).getTime() <= getServerNow()) {
      invalidReason = "expired";
    }
  }

  const backTo = order?.originalOrderId
    ? ROUTES.account.ticketDetail(order.originalOrderId)
    : ROUTES.account.tickets;

  const submitPayment = () => {
    applyPayment.mutate(
      { paymentMethod },
      {
        onSuccess: (res) => {
          window.location.href = res.data.paymentUrl;
        },
      },
    );
  };

  const cancelHold = () => {
    cancelOrder.mutate(
      { orderId, reason: CancelReason.CustomerChangedMind },
      { onSuccess: () => navigate(backTo, { replace: true }) },
    );
  };

  return {
    order,
    orderQuery,
    isLoading: orderQuery.isLoading,
    invalidReason,
    backTo,
    paymentMethod,
    setPaymentMethod,
    submitPayment,
    isSubmittingPayment: applyPayment.isPending,
    cancelHold,
    isCancelling: cancelOrder.isPending,
  };
}
