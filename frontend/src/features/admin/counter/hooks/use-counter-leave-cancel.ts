import { useCallback, useEffect, useRef } from "react";
import { CancelReason } from "@/types/enum";
import { counterApi } from "../api/counter-api";

const ABANDON_BODY = { reason: CancelReason.Other } as const;

interface UseCounterLeaveCancelParams {
  onRestoredAfterCancel: () => void;
}

export function useCounterLeaveCancel({
  onRestoredAfterCancel,
}: UseCounterLeaveCancelParams) {
  const cancellableIdRef = useRef<number | null>(null);
  const mountedRef = useRef(false);
  const cancelledOnHideRef = useRef(false);
  const onRestoredRef = useRef(onRestoredAfterCancel);

  useEffect(() => {
    onRestoredRef.current = onRestoredAfterCancel;
  }, [onRestoredAfterCancel]);

  const setCancellableOrderId = useCallback((id: number | null) => {
    cancellableIdRef.current = id;
  }, []);

  const cancelQuietly = useCallback(async (orderId: number) => {
    try {
      await counterApi.cancelOrder(orderId, ABANDON_BODY);
    } catch {
      // Best-effort: bỏ qua lỗi.
    }
  }, []);

  const hasLeft = useCallback(() => !mountedRef.current, []);

  useEffect(() => {
    mountedRef.current = true;

    const onPageHide = () => {
      const id = cancellableIdRef.current;
      if (id === null) return;
      cancellableIdRef.current = null;
      cancelledOnHideRef.current = true;
      counterApi.cancelOrderKeepalive(id, ABANDON_BODY);
    };
    const onPageShow = (event: PageTransitionEvent) => {
      if (!event.persisted || !cancelledOnHideRef.current) return;
      cancelledOnHideRef.current = false;
      onRestoredRef.current();
    };
    window.addEventListener("pagehide", onPageHide);
    window.addEventListener("pageshow", onPageShow);

    return () => {
      window.removeEventListener("pagehide", onPageHide);
      window.removeEventListener("pageshow", onPageShow);
      mountedRef.current = false;
      window.setTimeout(() => {
        if (mountedRef.current) return;
        const id = cancellableIdRef.current;
        if (id === null) return;
        cancellableIdRef.current = null;
        void cancelQuietly(id);
      }, 0);
    };
  }, [cancelQuietly]);

  return { setCancellableOrderId, cancelQuietly, hasLeft };
}

export type CounterLeaveCancel = ReturnType<typeof useCounterLeaveCancel>;
