import { Banknote } from "lucide-react";

import { DataTable } from "@/components/common/data-table/data-table";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useAdminCashRefundParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";

import { createCashRefundColumns } from "../components/cash-refund-columns";
import { useCashRefundConfirm } from "../hooks/use-cash-refund-confirm";
import { useCashRefunds } from "../hooks/use-cash-refunds";
import { CashRefundStatus } from "../types";
import { FilterBar, FilterSearch } from "@/components/common/filter/filter-bar";

const STATUS_TABS: { value: CashRefundStatus; label: string }[] = [
  { value: CashRefundStatus.Pending, label: "Chưa hoàn" },
  { value: CashRefundStatus.Refunded, label: "Đã hoàn" },
  { value: CashRefundStatus.All, label: "Tất cả" },
];

const AdminCashRefundsPage = () => {
  const [params, setParams] = useAdminCashRefundParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const confirmRefund = useCashRefundConfirm();

  const debouncedOrderCode = useDebouncedValue(params.orderCode);
  const refunds = useCashRefunds(
    stripNulls({
      status: params.status,
      orderCode: debouncedOrderCode || undefined,
      fromDate: params.fromDate,
      toDate: params.toDate,
      page: params.page,
      pageSize: params.pageSize,
    }),
  );

  const columns = createCashRefundColumns({ onConfirm: confirmRefund });

  const hasActiveFilters =
    Boolean(params.orderCode) || params.fromDate !== null || params.toDate !== null;

  return (
    <div>
      <PageHeader
        title="Đơn tiền mặt cần hoàn"
        description="Theo dõi các đơn tiền mặt bị rạp hủy suất và xác nhận khi đã hoàn tiền cho khách tại quầy."
      />

      <div className="mb-4 flex flex-wrap gap-2" role="group" aria-label="Lọc theo trạng thái hoàn tiền">
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
        <DateFilterPicker
          value={params.fromDate}
          onChange={(fromDate) => setParams({ fromDate, page: DEFAULT_PAGE })}
          placeholder="Từ ngày"
        />
        <DateFilterPicker
          value={params.toDate}
          onChange={(toDate) => setParams({ toDate, page: DEFAULT_PAGE })}
          placeholder="Đến ngày"
        />
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
          icon: Banknote,
          title:
            params.status === CashRefundStatus.Pending
              ? "Không còn đơn nào cần hoàn"
              : "Chưa có đơn tiền mặt cần hoàn",
          description: "Đơn tiền mặt hiện ở đây khi rạp hủy suất hoặc đóng phòng.",
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          setParams({ orderCode: "", fromDate: null, toDate: null, page: DEFAULT_PAGE })
        }
      />
    </div>
  );
};

export default AdminCashRefundsPage;
