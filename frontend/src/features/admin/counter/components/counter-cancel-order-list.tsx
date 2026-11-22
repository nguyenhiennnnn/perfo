import { Search, SearchX } from "lucide-react";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { BookingStatusBadge } from "@/components/common/status-badge";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { useCounterOrderSearch } from "../hooks/use-counter-cancel";
import type { CounterOrderListItem } from "../types/counter-cancel";
import { COUNTER_ORDER_SEARCH_MIN_LENGTH } from "../utils/cancel-order";

const ListSkeleton = () => (
  <div aria-busy="true" className="flex flex-col gap-2">
    {Array.from({ length: 4 }, (_, index) => (
      <CardSkeleton key={index} className="h-24" />
    ))}
  </div>
);

const getOrderSummary = (item: CounterOrderListItem) =>
  item.movieTitle
    ? `${item.movieTitle} · ${formatDateTime(item.startTime)} · ${item.ticketCount} vé`
    : "Combo bắp nước";

interface CounterCancelOrderListProps {
  search: string;
  selectedId: number | null;
  onSelect: (orderId: number) => void;
}

export const CounterCancelOrderList = ({
  search,
  selectedId,
  onSelect,
}: CounterCancelOrderListProps) => {
  const term = search.trim();
  const query = useCounterOrderSearch(term);

  if (term.length < COUNTER_ORDER_SEARCH_MIN_LENGTH) {
    return (
      <EmptyState
        icon={Search}
        title="Tìm đơn hàng cần hủy"
        description={`Nhập mã đơn hoặc số điện thoại khách (tối thiểu ${COUNTER_ORDER_SEARCH_MIN_LENGTH} ký tự).`}
      />
    );
  }

  return (
    <QueryState
      query={query}
      skeleton={<ListSkeleton />}
      isEmpty={(data) => data.items.length === 0}
      empty={
        <EmptyState
          icon={SearchX}
          title="Không tìm thấy đơn hàng"
          description="Kiểm tra lại mã đơn hoặc số điện thoại khách."
        />
      }
      errorTitle="Không tải được danh sách đơn hàng"
    >
      {(data) => (
        <div
          className={cn(
            "flex flex-col gap-2",
            query.isPlaceholderData && "opacity-60",
          )}
        >
          {data.items.map((item) => (
            <button
              key={item.orderId}
              type="button"
              aria-current={item.orderId === selectedId}
              onClick={() => onSelect(item.orderId)}
              className={cn(
                "flex w-full flex-col gap-2 cut-md p-3 text-left transition-colors",
                item.orderId === selectedId
                  ? "bg-primary/10"
                  : "bg-card hover:bg-muted",
              )}
            >
              <div className="flex items-center justify-between gap-2">
                <span className="t-code font-mono font-bold tracking-wider text-foreground">
                  {item.orderCode}
                </span>
                <BookingStatusBadge status={item.status} />
              </div>
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-foreground">
                    {item.customerName ?? "Khách vãng lai"}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {item.customerPhone ?? "Chưa có SĐT"}
                  </p>
                </div>
                <span className="shrink-0 text-sm font-semibold tabular-nums text-foreground">
                  {formatCurrency(item.finalAmount)}
                </span>
              </div>
              <p className="truncate text-xs text-muted-foreground">
                {getOrderSummary(item)}
              </p>
            </button>
          ))}
          {data.totalItems > data.items.length && (
            <p className="px-1 text-xs text-muted-foreground">
              Hiển thị {data.items.length}/{data.totalItems} đơn. Nhập chính xác
              hơn để thu hẹp kết quả.
            </p>
          )}
        </div>
      )}
    </QueryState>
  );
};
