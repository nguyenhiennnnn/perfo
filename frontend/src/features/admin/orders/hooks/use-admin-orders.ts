import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import type { CancelReason } from "@/types/enum";
import { ordersAdminApi } from "../api/orders-admin-api";
import type { AdminOrderListParams, OrderDetailDto } from "../types";

export function useAdminOrders(params: AdminOrderListParams) {
  return useQuery({
    queryKey: queryKeys.adminOrders.list(params),
    queryFn: () => ordersAdminApi.getOrders(params),
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

interface UseAdminOrderDetailOptions {
  getRefetchInterval?: (order: OrderDetailDto) => number | false;
}

export function useAdminOrderDetail(
  orderId: number,
  options?: UseAdminOrderDetailOptions,
) {
  const getRefetchInterval = options?.getRefetchInterval;
  return useQuery({
    queryKey: queryKeys.adminOrders.detail(orderId),
    queryFn: () => ordersAdminApi.getOrderDetail(orderId),
    enabled: orderId > 0,
    refetchInterval: getRefetchInterval
      ? (query) => {
          const order = query.state.data?.data;
          return order ? getRefetchInterval(order) : false;
        }
      : false,
    select: (res) => res.data,
  });
}

export function useCancelOrderInline() {
  return useMutation({
    meta: {
      invalidates: "bookingChanged",
      skipErrorToast: true,
      skipSuccessToast: true,
    },
    mutationFn: ({
      orderId,
      reason,
    }: {
      orderId: number;
      reason: CancelReason;
    }) => ordersAdminApi.cancelOrder(orderId, reason),
  });
}
