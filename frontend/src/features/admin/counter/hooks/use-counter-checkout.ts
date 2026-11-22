import { useState } from "react";
import { OrderStatus } from "@/types/enum";
import { useAdminOrderDetail } from "@/features/admin/orders/hooks/use-admin-orders";
import { useCounterCheckout } from "./use-counter-orders";
import type {
  CounterCheckoutRequest,
  CounterCheckoutResponse,
  CounterOrderResponse,
} from "../types";

const CONFIRM_POLL_INTERVAL_MS = 2000;

export function useCounterCheckoutFlow(
  order: CounterOrderResponse | null,
  isDone: boolean,
) {
  const [payments, setPayments] = useState<CounterCheckoutResponse[]>([]);

  const counterCheckout = useCounterCheckout();
  const orderDetail = useAdminOrderDetail(isDone && order ? order.orderId : 0, {
    getRefetchInterval: (detail) =>
      detail.status === OrderStatus.Pending ? CONFIRM_POLL_INTERVAL_MS : false,
  });

  const last = payments.at(-1) ?? null;
  const totalPaid = last?.totalPaid ?? 0;
  const outstandingAmount = last ? last.outstandingAmount : (order?.finalAmount ?? 0);
  const totalChange = payments.reduce((sum, p) => sum + p.change, 0);

  const pay = async (request: CounterCheckoutRequest, onFullyPaid: () => void) => {
    if (!order) return;
    try {
      const res = await counterCheckout.mutateAsync({
        orderId: order.orderId,
        ...request,
      });
      setPayments((prev) => [...prev, res.data]);
      if (res.data.isFullyPaid) onFullyPaid();
    } catch {
      return;
    }
  };

  const clearCheckout = () => setPayments([]);

  return {
    payments,
    totalPaid,
    outstandingAmount,
    totalChange,
    orderDetail,
    pay,
    clearCheckout,
    isPaying: counterCheckout.isPending,
  };
}
