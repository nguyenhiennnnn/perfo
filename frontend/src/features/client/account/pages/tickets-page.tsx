import { useId, useState } from "react";
import { Receipt } from "lucide-react";
import { Chip, ChipGroup } from "@/components/common/chip";
import { EmptyState } from "@/components/common/empty-state";
import { PageTitle } from "@/components/common/page-title";
import { QueryState } from "@/components/common/query-state";
import { ButtonLink } from "@/components/common/button-link";
import { SkeletonList } from "@/components/common/skeleton-list";
import { InfiniteScrollTrigger } from "@/components/common/infinite-scroll-trigger";
import { OrderCard } from "@/features/client/orders/components/history/order-card";
import { useMyOrders } from "@/features/client/orders/hooks/use-orders";
import { OrderStatus } from "@/types/enum";
import { ACCOUNT_LIST_PAGE_LIMIT, SKELETON_LIST_COUNT } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";

const TAB_ALL_KEY = "all";

const TABS: { value: string | null; label: string }[] = [
  { value: null, label: "Tất cả" },
  { value: OrderStatus.Pending, label: "Chờ thanh toán" },
  { value: OrderStatus.Confirmed, label: "Đã xác nhận" },
  { value: OrderStatus.Cancelled, label: "Đã hủy" },
  { value: OrderStatus.Expired, label: "Hết hạn" },
];

const AccountTicketsPage = () => {
  const [status, setStatus] = useState<string | null>(null);
  const tabsId = useId();
  const ordersQuery = useMyOrders({ status: status ?? undefined, limit: ACCOUNT_LIST_PAGE_LIMIT });
  const { data, hasNextPage, isFetchingNextPage, fetchNextPage } = ordersQuery;

  const orders = data?.orders ?? [];
  const getTabId = (value: string | null) => `${tabsId}-${value ?? TAB_ALL_KEY}`;

  return (
    <div>
      <PageTitle title="Lịch sử đơn" description="Các đơn bạn đã đặt, theo thứ tự mới nhất." />

      <ChipGroup role="tablist" aria-label="Trạng thái đơn">
        {TABS.map((tab) => (
          <Chip
            key={tab.label}
            role="tab"
            id={getTabId(tab.value)}
            selected={status === tab.value}
            onClick={() => setStatus(tab.value)}
          >
            {tab.label}
          </Chip>
        ))}
      </ChipGroup>

      <div role="tabpanel" aria-labelledby={getTabId(status)} className="mt-5 space-y-3" aria-busy={ordersQuery.isLoading || undefined}>
        <QueryState
          query={ordersQuery}
          skeleton={<SkeletonList count={SKELETON_LIST_COUNT} className="h-44 sm:h-32" />}
          isEmpty={orders.length === 0}
          empty={
            status ? (
              <EmptyState
                variant="filtered"
                title="Không có đơn ở trạng thái này"
                description="Chọn trạng thái khác hoặc xem tất cả đơn."
                onClearFilters={() => setStatus(null)}
              />
            ) : (
              <EmptyState
                icon={Receipt}
                title="Chưa có đơn nào"
                description="Đặt vé xem phim để đơn hàng hiện ở đây."
                action={<ButtonLink to={ROUTES.movies.list} variant="secondary">Xem danh sách phim</ButtonLink>}
              />
            )
          }
          errorTitle="Không tải được lịch sử đơn"
        >
          {orders.map((order) => (
            <OrderCard key={order.orderId} order={order} />
          ))}
          <InfiniteScrollTrigger hasNextPage={hasNextPage} isFetchingNextPage={isFetchingNextPage} fetchNextPage={fetchNextPage} />
        </QueryState>
      </div>
    </div>
  );
}

export default AccountTicketsPage;