import { useInfiniteQuery, useMutation, useQuery } from "@tanstack/react-query";
import { ordersApi } from "../api/orders-api";
import { queryKeys } from "@/api/query-keys";
import { OrderStatus } from "@/types/enum";
import type {
  HoldSeatsRequest,
  CheckoutRequest,
  CancelOrderRequest,
  OrderListParams,
} from "../types";

export function useMyOrders(params: Omit<OrderListParams, "cursor">) {
  return useInfiniteQuery({
    queryKey: queryKeys.orders.myList(params),
    queryFn: ({ pageParam }) =>
      ordersApi.getMyOrders({ ...params, cursor: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.data.hasMore
        ? (lastPage.data.nextCursor ?? undefined)
        : undefined,
    select: (data) => ({
      orders: data.pages.flatMap((p) => p.data.items),
      hasMore: data.pages.at(-1)?.data.hasMore ?? false,
    }),
  });
}

export function useOrderDetail(
  orderId: number,
  options?: { pollWhilePendingMs?: number },
) {
  const pollWhilePendingMs = options?.pollWhilePendingMs;
  return useQuery({
    queryKey: queryKeys.orders.detail(orderId),
    queryFn: () => ordersApi.getOrderDetail(orderId),
    enabled: orderId > 0,
    refetchInterval: pollWhilePendingMs
      ? (query) =>
          query.state.data?.data.status === OrderStatus.Pending
            ? pollWhilePendingMs
            : false
      : false,
    refetchIntervalInBackground: true,
    select: (res) => res.data,
  });
}

export function useHoldSeats() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: HoldSeatsRequest) => ordersApi.holdSeats(body),
  });
}

export function useApplyPayment(orderId: number) {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: CheckoutRequest) =>
      ordersApi.applyPayment(orderId, body),
  });
}

export function useCancelOrder() {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: ({
      orderId,
      ...body
    }: CancelOrderRequest & { orderId: number }) =>
      ordersApi.cancelOrder(orderId, body),
  });
}
