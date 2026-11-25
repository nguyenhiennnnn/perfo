import { ArrowRightLeft, X } from "lucide-react";

import { DataTable } from "@/components/common/data-table/data-table";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useAdminRoomChangeRefundParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";

import { createRoomChangeRefundColumns } from "../components/room-change-refund-columns";
import { useRoomChangeRefundConfirm } from "../hooks/use-room-change-refund-confirm";
import { useRoomChangeRefunds } from "../hooks/use-room-change-refunds";
import { RoomChangeRefundStatus } from "../types";
import { FilterBar, FilterSearch } from "@/components/common/filter/filter-bar";

const STATUS_TABS: { value: RoomChangeRefundStatus; label: string }[] = [
  { value: RoomChangeRefundStatus.Pending, label: "Chờ hoàn" },
  { value: RoomChangeRefundStatus.Refunded, label: "Đã hoàn" },
  { value: RoomChangeRefundStatus.Voided, label: "Đã hủy" },
  { value: RoomChangeRefundStatus.All, label: "Tất cả" },
];

const AdminRoomChangeRefundsPage = () => {
  const [params, setParams] = useAdminRoomChangeRefundParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const confirmRefund = useRoomChangeRefundConfirm();

  const debouncedOrderCode = useDebouncedValue(params.orderCode);
  const refunds = useRoomChangeRefunds(
    stripNulls({
      status: params.status,
      showtimeId: params.showtimeId,
      orderCode: debouncedOrderCode || undefined,
      page: params.page,
      pageSize: params.pageSize,
    }),
  );

  const columns = createRoomChangeRefundColumns({ onConfirm: confirmRefund });

  const hasActiveFilters = Boolean(params.orderCode) || params.showtimeId !== null;

  return (
    <div>
      <PageHeader
        title="Hoàn chênh lệch đổi phòng"
        description="Theo dõi các khoản hoàn chênh lệch khi khách được chuyển sang ghế rẻ hơn do đổi phòng và xác nhận hoàn tiền."
      />

      <div
        className="mb-4 flex flex-wrap gap-2"
        role="group"
        aria-label="Lọc theo trạng thái hoàn tiền"
      >
        {STATUS_TABS.map((tab) => (
          <Button
            key={tab.value}
            size="sm"
            variant={params.status === tab.value ? "default" : "outline"}
            aria-pressed={params.status === tab.value}
            onClick={() => setParams({ status: tab.value, page: DEFAULT_PAGE })}
          >
            {tab.label}
          </Button>
        ))}
      </div>

      <FilterBar className="mb-4">
        <FilterSearch
          placeholder="Tìm theo mã đơn..."
          value={params.orderCode}
          onChange={(value) => setParams({ orderCode: value, page: DEFAULT_PAGE })}
        />
        {params.showtimeId !== null && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => setParams({ showtimeId: null, page: DEFAULT_PAGE })}
          >
            Suất chiếu #{params.showtimeId}
            <X className="size-3.5" />
          </Button>
        )}
      </FilterBar>

      <DataTable
        columns={columns}
        data={refunds.data?.items ?? []}
        isLoading={refunds.isLoading}
        isError={refunds.isError}
        error={refunds.error}
        onRetry={() => void refunds.refetch()}
        isRefreshing={refunds.isPlaceholderData}
        totalItems={refunds.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: ArrowRightLeft,
          title:
            params.status === RoomChangeRefundStatus.Pending
              ? "Không còn khoản nào cần hoàn"
              : "Chưa có khoản hoàn chênh lệch",
          description: "Khoản hoàn hiện ở đây khi suất chiếu được chuyển sang phòng có ghế rẻ hơn.",
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() => setParams({ orderCode: "", showtimeId: null, page: DEFAULT_PAGE })}
      />
    </div>
  );
};

export default AdminRoomChangeRefundsPage;
