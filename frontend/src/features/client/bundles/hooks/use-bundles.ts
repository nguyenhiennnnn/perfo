import { useCallback, useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { toast } from "@/components/ui/toast";
import { queryKeys } from "@/api/query-keys";
import type { ShowtimeSeatItemDto } from "@/features/client/showtimes/types";
import { bundlesApi } from "../api/bundles-api";
import type { BundleSelectionRequest } from "../types";
import {
  assignBundleTicketTypes,
  findBundleSeatRejection,
  getBundleMaxQuantity,
  getBundleSeatRejectionMessage,
} from "../utils/bundle-selection";

export function useShowtimeBundles(showtimeId: number) {
  return useQuery({
    queryKey: queryKeys.bundles.forShowtime(showtimeId),
    queryFn: () => bundlesApi.getForShowtime(showtimeId),
    enabled: showtimeId > 0,
    refetchOnMount: "always",
    select: (res) => res.data,
  });
}

export function useBundleSelection(showtimeId: number) {
  const bundles = useShowtimeBundles(showtimeId);
  const [selection, setSelection] = useState<BundleSelectionRequest | null>(null);

  const selectedBundle = useMemo(
    () => bundles.data?.find((bundle) => bundle.bundleId === selection?.bundleId) ?? null,
    [bundles.data, selection?.bundleId],
  );

  const quantity = selectedBundle && selection
    ? Math.min(selection.quantity, getBundleMaxQuantity(selectedBundle))
    : 0;
  const requiredSeats = selectedBundle ? selectedBundle.ticketCount * quantity : 0;

  useEffect(() => {
    if (!selection || !bundles.data || selectedBundle) return;
    setSelection(null);
    toast.add({
      title: "Gói đã chọn không còn áp dụng cho suất này và được bỏ khỏi lựa chọn.",
      type: "warning",
    });
  }, [bundles.data, selection, selectedBundle]);

  const select = useCallback((bundleId: number) => {
    setSelection((current) => (current?.bundleId === bundleId ? null : { bundleId, quantity: 1 }));
  }, []);

  const setQuantity = useCallback((next: number) => {
    setSelection((current) => (current ? { ...current, quantity: next } : current));
  }, []);

  const clear = useCallback(() => setSelection(null), []);

  const assignTicketTypes = useCallback(
    (seatIds: number[]) =>
      selectedBundle ? assignBundleTicketTypes(seatIds, selectedBundle, quantity) : null,
    [selectedBundle, quantity],
  );

  const guardSeatChange = useCallback(
    (current: Set<number>, next: Set<number>, seats: ShowtimeSeatItemDto[]) => {
      if (!selectedBundle) return true;
      const reason = findBundleSeatRejection(selectedBundle, requiredSeats, current, next, seats);
      if (!reason) return true;
      toast.add({
        title: getBundleSeatRejectionMessage(selectedBundle, reason, requiredSeats),
        type: "warning",
      });
      return false;
    },
    [selectedBundle, requiredSeats],
  );

  const payload: BundleSelectionRequest[] | undefined = selectedBundle
    ? [{ bundleId: selectedBundle.bundleId, quantity }]
    : undefined;

  return {
    bundles,
    selectedBundle,
    quantity,
    requiredSeats,
    payload,
    select,
    setQuantity,
    clear,
    assignTicketTypes,
    guardSeatChange,
  };
}

export type BundleSelection = ReturnType<typeof useBundleSelection>;
