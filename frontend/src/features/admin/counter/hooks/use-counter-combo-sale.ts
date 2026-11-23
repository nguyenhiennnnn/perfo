import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "@/components/ui/toast";
import { COUNTER_WALK_IN_NAME } from "@/config/default-constants";
import { useActiveCombos } from "@/features/client/combos/hooks/use-combos";
import { formatCurrency } from "@/lib/formatters";
import { CancelReason } from "@/types/enum";
import { ordersAdminApi } from "@/features/admin/orders/api/orders-admin-api";
import {
  CounterPaymentMethod,
  type CounterCheckoutResponse,
  type CounterOrderResponse,
} from "../types";
import {
  useCounterCancelOrder,
  useCounterCheckout,
  useCounterComboOrder,
} from "./use-counter-orders";

export type CounterComboStep = "build" | "payment" | "done";

export interface CounterComboLine {
  comboId: number;
  name: string;
  quantity: number;
  unitPrice: number;
}

const isOrderExpired = (order: CounterOrderResponse | null) =>
  order !== null && new Date(order.expiresAt).getTime() <= Date.now();

export function useCounterComboSale() {
  const activeCombos = useActiveCombos();
  const createOrder = useCounterComboOrder();
  const checkout = useCounterCheckout();
  const cancelOrder = useCounterCancelOrder();

  const [step, setStep] = useState<CounterComboStep>("build");
  const [quantities, setQuantities] = useState<Record<number, number>>({});
  const [customerName, setCustomerName] = useState(COUNTER_WALK_IN_NAME);
  const [customerPhone, setCustomerPhone] = useState("");
  const [userId, setUserId] = useState<number | null>(null);
  const [order, setOrder] = useState<CounterOrderResponse | null>(null);
  const [orderLines, setOrderLines] = useState<CounterComboLine[]>([]);
  const [receipt, setReceipt] = useState<CounterCheckoutResponse | null>(null);

  const mountedRef = useRef(false);
  const pendingOrderIdRef = useRef<number | null>(null);
  const cancelledOnHideRef = useRef(false);
  const resetRef = useRef<() => void>(() => undefined);

  const draftLines = useMemo<CounterComboLine[]>(
    () =>
      (activeCombos.data ?? [])
        .map((combo) => ({
          comboId: combo.comboId,
          name: combo.name,
          quantity: quantities[combo.comboId] ?? 0,
          unitPrice: combo.price,
        }))
        .filter((line) => line.quantity > 0),
    [activeCombos.data, quantities],
  );

  const draftTotal = draftLines.reduce(
    (sum, line) => sum + line.unitPrice * line.quantity,
    0,
  );
  const canCreateOrder =
    draftLines.length > 0 &&
    (userId !== null || customerName.trim().length > 0);

  const setComboQuantity = (comboId: number, quantity: number) =>
    setQuantities((prev) => ({ ...prev, [comboId]: quantity }));

  const resetLocal = useCallback(() => {
    setStep("build");
    setQuantities({});
    setCustomerName(COUNTER_WALK_IN_NAME);
    setCustomerPhone("");
    setUserId(null);
    setOrder(null);
    setOrderLines([]);
    setReceipt(null);
    pendingOrderIdRef.current = null;
  }, []);

  useEffect(() => {
    resetRef.current = resetLocal;
  }, [resetLocal]);

  useEffect(() => {
    mountedRef.current = true;

    const onPageHide = () => {
      const id = pendingOrderIdRef.current;
      if (id === null) return;
      pendingOrderIdRef.current = null;
      cancelledOnHideRef.current = true;
      ordersAdminApi.cancelOrderKeepalive(id, CancelReason.Other);
    };
    const onPageShow = (event: PageTransitionEvent) => {
      if (!event.persisted || !cancelledOnHideRef.current) return;
      cancelledOnHideRef.current = false;
      resetRef.current();
    };
    window.addEventListener("pagehide", onPageHide);
    window.addEventListener("pageshow", onPageShow);

    return () => {
      window.removeEventListener("pagehide", onPageHide);
      window.removeEventListener("pageshow", onPageShow);
      mountedRef.current = false;
      window.setTimeout(() => {
        if (mountedRef.current) return;
        const id = pendingOrderIdRef.current;
        if (id === null) return;
        pendingOrderIdRef.current = null;
        void ordersAdminApi.cancelOrder(id, CancelReason.Other).catch(() => undefined);
      }, 0);
    };
  }, []);

  const submitOrder = async () => {
    if (!canCreateOrder) return;
    try {
      const res = await createOrder.mutateAsync({
        combos: draftLines.map(({ comboId, quantity }) => ({ comboId, quantity })),
        userId,
        customerName: customerName.trim(),
        customerPhone: customerPhone.trim() || undefined,
      });
      if (!mountedRef.current) {
        void ordersAdminApi
          .cancelOrder(res.data.orderId, CancelReason.Other)
          .catch(() => undefined);
        return;
      }
      pendingOrderIdRef.current = res.data.orderId;
      setOrder(res.data);
      setOrderLines(draftLines);
      setStep("payment");
    } catch {
      return;
    }
  };

  const pay = async (
    method: CounterPaymentMethod,
    amountReceived?: number,
  ) => {
    if (!order) return;
    try {
      const res = await checkout.mutateAsync({
        orderId: order.orderId,
        ...(method === CounterPaymentMethod.Cash
          ? { paymentMethod: method, amountReceived }
          : { paymentMethod: method }),
      });
      if (!res.data.isFullyPaid) {
        toast.add({
          title: `Đơn còn thiếu ${formatCurrency(res.data.outstandingAmount)}.`,
          type: "warning",
        });
        return;
      }
      pendingOrderIdRef.current = null;
      setReceipt(res.data);
      setStep("done");
    } catch {
      return;
    }
  };

  const releaseOrder = async (reason: CancelReason) => {
    const id = pendingOrderIdRef.current;
    if (id !== null && !isOrderExpired(order)) {
      try {
        await cancelOrder.mutateAsync({ orderId: id, reason });
      } catch {
        return false;
      }
    }
    pendingOrderIdRef.current = null;
    setOrder(null);
    setOrderLines([]);
    return true;
  };

  const backToBuild = async () => {
    if (!(await releaseOrder(CancelReason.CustomerChangedMind))) return;
    setStep("build");
  };

  const reset = async () => {
    if (step === "payment" && !(await releaseOrder(CancelReason.CustomerChangedMind)))
      return;
    resetLocal();
  };

  return {
    step,
    combos: activeCombos,
    quantities,
    setComboQuantity,
    customerName,
    setCustomerName,
    customerPhone,
    setCustomerPhone,
    userId,
    setUserId,
    draftLines,
    draftTotal,
    canCreateOrder,
    submitOrder,
    isCreatingOrder: createOrder.isPending,
    order,
    orderLines,
    pay,
    isPaying: checkout.isPending,
    isReleasing: cancelOrder.isPending,
    receipt,
    backToBuild,
    reset,
    hasPendingOrder: step === "payment",
    isDirty: step !== "build" || draftLines.length > 0,
  };
}

export type CounterComboSaleFlow = ReturnType<typeof useCounterComboSale>;
