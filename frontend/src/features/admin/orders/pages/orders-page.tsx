import { useNavigate } from "react-router";
import { ReceiptText } from "lucide-react";

import { ENDPOINTS } from "@/api/endpoints";
import { DataTable } from "@/components/common/data-table/data-table";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { ExportButton } from "@/components/common/export-button";
import { PageHeader } from "@/components/common/page-header";
import { Input } from "@/components/ui/input";
import { ALL_STATUSES_LABEL, DEFAULT_PAGE, STATUS_LABEL } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useAdminOrderParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { bookingStatusOptions, orderChannelOptions, orderTypeOptions } from "@/lib/enum-options";
import { stripNulls } from "@/lib/query-params";

import { createOrderColumns } from "../components/order-columns";
import { useAdminOrders } from "../hooks/use-admin-orders";
import type { AdminOrderListItemDto } from "../types";

const AdminOrdersPage = () => {
  const [params, setParams] = useAdminOrderParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const navigate = useNavigate();

  const listParams = stripNulls({ ...params, search: useDebouncedValue(params.search) });
  const orders = useAdminOrders(listParams);

  const columns = createOrderColumns({
    onViewDetail: (order: AdminOrderListItemDto) =>
      navigate(ROUTES.admin.orderDetail(order.orderId)),
  });

  const hasActiveFilters =
    Boolean(params.search) ||
    params.status !== null ||
    params.channel !== null ||
    params.type !== null ||
    params.fromDate !== null ||
    params.toDate !== null;

  return (
    <div>
      <PageHeader
        title="Đơn hàng"
        description="Theo dõi đơn hàng, kênh bán và trạng thái xử lý."
        action={
          <ExportButton
            endpoint={ENDPOINTS.admin.bookings.export}
            jobEndpoint={ENDPOINTS.admin.bookings.exportJobs}
            params={{
              search: listParams.search || undefined,
              status: listParams.status,
              channel: listParams.channel,
              type: listParams.type,
              fromDate: listParams.fromDate,
              toDate: listParams.toDate,
            }}
            fallbackFilename="orders"
            size="default"
          />
        }
      />

      <div className="mb-4 grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
        <Input
          placeholder="Tìm theo mã đơn, tên khách, số điện thoại..."
          value={params.search}
          onChange={(e) => setParams({ search: e.target.value, page: DEFAULT_PAGE })}
          className="col-span-2 md:col-span-3 xl:col-span-2"
        />
        <EnumSelectFilter
          value={params.status}
          onChange={(status) => setParams({ status, page: DEFAULT_PAGE })}
          options={bookingStatusOptions}
          placeholder={STATUS_LABEL}
          allLabel={ALL_STATUSES_LABEL}
        />
        <EnumSelectFilter
          value={params.channel}
          onChange={(channel) => setParams({ channel, page: DEFAULT_PAGE })}
          options={orderChannelOptions}
          placeholder="Kênh bán"
          allLabel="Tất cả kênh"
        />
        <EnumSelectFilter
          value={params.type}
          onChange={(type) => setParams({ type, page: DEFAULT_PAGE })}
          options={orderTypeOptions}
          placeholder="Loại đơn"
          allLabel="Tất cả loại đơn"
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
      </div>

      <DataTable
        columns={columns}
        data={orders.data?.items ?? []}
        isLoading={orders.isLoading}
        isError={orders.isError}
        error={orders.error}
        onRetry={() => void orders.refetch()}
        isRefreshing={orders.isPlaceholderData}
        totalItems={orders.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: ReceiptText,
          title: "Chưa có đơn hàng",
          description: "Đơn hàng hiện ở đây sau khi khách hoặc quầy tạo đơn.",
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          setParams({
            search: "",
            status: null,
            channel: null,
            type: null,
            fromDate: null,
            toDate: null,
            page: DEFAULT_PAGE,
          })
        }
      />
    </div>
  );
};

export default AdminOrdersPage;
