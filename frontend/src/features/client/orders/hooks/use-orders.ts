import { useInfiniteQuery, useMutation, useQuery } from "@tanstack/react-query";
import { ordersApi } from "../api/orders-api";
import { queryKeys } from "@/api/query-keys";
import type {
  ApplyPromotionRequest,
  HoldSeatsRequest,
  CheckoutRequest,
  CancelOrderRequest,
  OrderDetailDto,
  OrderListParams,
  ComboSupplementRequest,
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

interface UseOrderDetailOptions {
  getRefetchInterval?: (order: OrderDetailDto) => number | false;
  alwaysRefetchOnMount?: boolean;
}

export function useOrderDetail(orderId: number, options?: UseOrderDetailOptions) {
  const { getRefetchInterval, alwaysRefetchOnMount } = options ?? {};
  return useQuery({
    queryKey: queryKeys.orders.detail(orderId),
    queryFn: () => ordersApi.getOrderDetail(orderId),
    enabled: orderId > 0,
    ...(alwaysRefetchOnMount && { refetchOnMount: "always" as const }),
    refetchInterval: getRefetchInterval
      ? (query) => {
          const order = query.state.data?.data;
          return order ? getRefetchInterval(order) : false;
        }
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

export function useOrderComboSuggestions(orderId: number, enabled = true) {
  return useQuery({
    queryKey: queryKeys.orders.comboSuggestions(orderId),
    queryFn: () => ordersApi.getComboSuggestions(orderId),
    enabled: enabled && orderId > 0,
    refetchOnMount: "always",
    select: (res) => res.data,
  });
}

export function useCreateComboSupplement(orderId: number) {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: ComboSupplementRequest) => ordersApi.createComboSupplement(orderId, body),
  });
}

export function useApplyPayment(orderId: number) {
  return useMutation({
    meta: { invalidates: "bookingChanged" },
    mutationFn: (body: CheckoutRequest) =>
      ordersApi.applyPayment(orderId, body),
  });
}

export function useApplyPromotion(orderId: number) {
  return useMutation({
    meta: { invalidates: "orderPromotionChanged", skipErrorToast: true },
    mutationFn: (body: ApplyPromotionRequest) =>
      ordersApi.applyPromotion(orderId, body),
  });
}

export function useRemovePromotion(orderId: number) {
  return useMutation({
    meta: { invalidates: "orderPromotionChanged", skipErrorToast: true },
    mutationFn: () => ordersApi.removePromotion(orderId),
  });
}

export function useCancelOrder(options?: { inlineError?: boolean }) {
  return useMutation({
    meta: { invalidates: "bookingChanged", skipErrorToast: options?.inlineError },
    mutationFn: ({
      orderId,
      ...body
    }: CancelOrderRequest & { orderId: number }) =>
      ordersApi.cancelOrder(orderId, body),
  });
}
