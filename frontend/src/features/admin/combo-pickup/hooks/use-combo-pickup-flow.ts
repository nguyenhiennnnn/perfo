import { useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { toast } from "@/components/ui/toast";
import { extractApiError } from "@/lib/error";
import type { ComboPickupOrderDto } from "../types";
import { useComboPickupLookup, useComboPickupSubmit } from "./use-combo-pickup";

export const COMBO_PICKUP_CODE_PARAM = "code";

export type ComboPickupScanSource = "camera" | "manual";

const buildSelection = (order: ComboPickupOrderDto): Record<number, number> =>
  Object.fromEntries(
    order.lines
      .filter((line) => line.remainingQuantity > 0)
      .map((line) => [line.orderLineId, line.remainingQuantity]),
  );

export function useComboPickupFlow() {
  const lookup = useComboPickupLookup();
  const submit = useComboPickupSubmit();
  const [searchParams, setSearchParams] = useSearchParams();

  const [code, setCode] = useState("");
  const [cameraActive, setCameraActive] = useState(false);
  const [order, setOrder] = useState<ComboPickupOrderDto | null>(null);
  const [selection, setSelection] = useState<Record<number, number>>({});
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [lastPickedUp, setLastPickedUp] = useState<number | null>(null);

  const scannedRef = useRef<string | null>(null);
  const initialHandledRef = useRef(false);

  const applyOrder = useCallback((next: ComboPickupOrderDto) => {
    setOrder(next);
    setSelection(buildSelection(next));
  }, []);

  const findOrder = useCallback(
    (raw: string, source: ComboPickupScanSource) => {
      const value = raw.trim();
      if (!value || lookup.isPending || submit.isPending) return;
      if (source === "camera" && value === scannedRef.current) return;

      scannedRef.current = value;
      lookup.mutate(value, {
        onSuccess: (res) => {
          applyOrder(res.data);
          setLookupError(null);
          setLastPickedUp(null);
          setCode("");
        },
        onError: (err) => {
          scannedRef.current = null;
          setOrder(null);
          setSelection({});
          setLastPickedUp(null);
          setLookupError(extractApiError(err).message);
        },
      });
    },
    [applyOrder, lookup, submit.isPending],
  );

  useEffect(() => {
    const initialCode = searchParams.get(COMBO_PICKUP_CODE_PARAM);
    if (!initialCode || initialHandledRef.current) return;
    initialHandledRef.current = true;
    setSearchParams({}, { replace: true });
    findOrder(initialCode, "manual");
  }, [findOrder, searchParams, setSearchParams]);

  const setLineQuantity = (orderLineId: number, quantity: number) =>
    setSelection((prev) => ({ ...prev, [orderLineId]: quantity }));

  const selectedTotal = Object.values(selection).reduce((sum, quantity) => sum + quantity, 0);

  const confirmPickup = () => {
    if (!order || selectedTotal <= 0) return;
    const items = order.lines
      .map((line) => ({
        orderLineId: line.orderLineId,
        quantity: selection[line.orderLineId] ?? 0,
      }))
      .filter((item) => item.quantity > 0);

    submit.mutate(
      { code: order.orderCode, items },
      {
        onSuccess: (res) => {
          applyOrder(res.data.order);
          setLastPickedUp(res.data.quantityPickedUp);
          toast.add({
            title: `Đã giao ${res.data.quantityPickedUp} phần combo.`,
            type: "success",
          });
        },
        onError: () => {
          lookup.mutate(order.orderCode, { onSuccess: (res) => applyOrder(res.data) });
        },
      },
    );
  };

  const reset = () => {
    scannedRef.current = null;
    setOrder(null);
    setSelection({});
    setLookupError(null);
    setLastPickedUp(null);
    setCode("");
  };

  return {
    code,
    setCode,
    cameraActive,
    toggleCamera: () => setCameraActive((prev) => !prev),
    order,
    selection,
    setLineQuantity,
    selectedTotal,
    lookupError,
    lastPickedUp,
    findOrder,
    confirmPickup,
    reset,
    isLooking: lookup.isPending,
    isSubmitting: submit.isPending,
  };
}

export type ComboPickupFlow = ReturnType<typeof useComboPickupFlow>;
