import { useCallback, useRef, useState } from "react";

import { toast } from "@/components/ui/toast";
import { extractApiError } from "@/lib/error";

import type { EventGiftHandoverTicketDto } from "../types";
import { useEventGiftHandOver, useEventGiftLookup } from "./use-event-gift-handover";

export type EventGiftScanSource = "camera" | "manual";

export function useEventGiftHandoverFlow() {
  const lookup = useEventGiftLookup();
  const handOver = useEventGiftHandOver();

  const [code, setCode] = useState("");
  const [cameraActive, setCameraActive] = useState(false);
  const [ticket, setTicket] = useState<EventGiftHandoverTicketDto | null>(null);
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [lastHanded, setLastHanded] = useState<number | null>(null);

  const scannedRef = useRef<string | null>(null);
  const qrRef = useRef<string>("");

  const findTicket = useCallback(
    (raw: string, source: EventGiftScanSource) => {
      const value = raw.trim();
      if (!value || lookup.isPending || handOver.isPending) return;
      if (source === "camera" && value === scannedRef.current) return;

      scannedRef.current = value;
      lookup.mutate(value, {
        onSuccess: (res) => {
          qrRef.current = value;
          setTicket(res.data);
          setLookupError(null);
          setLastHanded(null);
          setCode("");
        },
        onError: (err) => {
          scannedRef.current = null;
          setTicket(null);
          setLastHanded(null);
          setLookupError(extractApiError(err).message);
        },
      });
    },
    [lookup, handOver.isPending],
  );

  const confirmHandOver = () => {
    if (!ticket || ticket.totalRemaining <= 0 || handOver.isPending) return;
    handOver.mutate(qrRef.current, {
      onSuccess: (res) => {
        setTicket(res.data.ticket);
        setLastHanded(res.data.handedQuantity);
        toast.add({ title: `Đã trao ${res.data.handedQuantity} quà.`, type: "success" });
      },
      onError: () => {
        lookup.mutate(qrRef.current, { onSuccess: (res) => setTicket(res.data) });
      },
    });
  };

  const reset = () => {
    scannedRef.current = null;
    qrRef.current = "";
    setTicket(null);
    setLookupError(null);
    setLastHanded(null);
    setCode("");
  };

  return {
    code,
    setCode,
    cameraActive,
    toggleCamera: () => setCameraActive((prev) => !prev),
    ticket,
    lookupError,
    lastHanded,
    findTicket,
    confirmHandOver,
    reset,
    isLooking: lookup.isPending,
    isSubmitting: handOver.isPending,
  };
}

export type EventGiftHandoverFlow = ReturnType<typeof useEventGiftHandoverFlow>;
