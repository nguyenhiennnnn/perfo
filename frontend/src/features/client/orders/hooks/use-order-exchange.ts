import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import type { OrderExchangeAdapter, StartOrderExchangeRequest } from "../types";

export function useExchangePreview(
  adapter: OrderExchangeAdapter,
  orderId: number,
  lineIds: number[],
  enabled = true,
) {
  return useQuery({
    queryKey: queryKeys.orders.exchangePreview(adapter.channel, orderId, lineIds),
    queryFn: () => adapter.preview(orderId, { orderLineIds: lineIds }),
    enabled: enabled && orderId > 0 && lineIds.length > 0,
    staleTime: 0,
    refetchOnMount: "always",
    select: (res) => res.data,
  });
}

export function useStartExchange(adapter: OrderExchangeAdapter) {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: ({ orderId, body }: { orderId: number; body: StartOrderExchangeRequest }) =>
      adapter.start(orderId, body),
  });
}
