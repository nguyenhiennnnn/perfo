import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/api/query-keys";
import { counterCancelApi } from "../api/counter-cancel-api";
import {
  COUNTER_ORDER_SEARCH_MIN_LENGTH,
  COUNTER_ORDER_SEARCH_PAGE_SIZE,
} from "../utils/cancel-order";

export function useCounterOrderSearch(search: string) {
  const term = search.trim();
  const params = {
    search: term,
    page: 1,
    pageSize: COUNTER_ORDER_SEARCH_PAGE_SIZE,
  };

  return useQuery({
    queryKey: queryKeys.adminOrders.list({ scope: "counter-cancel", ...params }),
    queryFn: () => counterCancelApi.searchOrders(params),
    enabled: term.length >= COUNTER_ORDER_SEARCH_MIN_LENGTH,
    placeholderData: keepPreviousData,
    select: (res) => res.data,
  });
}

export function useCounterOrderDetail(orderId: number) {
  return useQuery({
    queryKey: queryKeys.adminOrders.detail(orderId),
    queryFn: () => counterCancelApi.getOrderDetail(orderId),
    enabled: orderId > 0,
    select: (res) => res.data,
  });
}
