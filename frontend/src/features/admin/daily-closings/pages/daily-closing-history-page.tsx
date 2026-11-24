import { Archive } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { ButtonLink } from "@/components/common/button-link";
import { stripNulls } from "@/lib/query-params";
import { ROUTES } from "@/config/routes";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminDailyClosingHistoryParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";

import { useAdminDailyClosingList } from "../hooks/use-admin-daily-closings";
import { createDailyClosingHistoryColumns } from "../components/daily-closing-history-columns";

const columns = createDailyClosingHistoryColumns();

const AdminDailyClosingHistoryPage = () => {
  const [params, setParams] = useAdminDailyClosingHistoryParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const closings = useAdminDailyClosingList(stripNulls(params));

  return (
    <div>
      <PageHeader
        title="Lịch sử chốt ngày"
        description="Tra cứu báo cáo chốt của các ngày đã chốt."
        action={
          <ButtonLink to={ROUTES.admin.dailyClosings} variant="outline">
            Chốt ngày mới
          </ButtonLink>
        }
      />

      <div className="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
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
        data={closings.data?.items ?? []}
        isLoading={closings.isLoading}
        isError={closings.isError}
        error={closings.error}
        onRetry={() => void closings.refetch()}
        isRefreshing={closings.isPlaceholderData}
        totalItems={closings.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: Archive,
          title: "Chưa có ngày nào được chốt",
          description: "Báo cáo chốt xuất hiện sau khi quản trị chốt doanh thu một ngày.",
        }}
        hasActiveFilters={params.fromDate !== null || params.toDate !== null}
        onClearFilters={() => setParams({ fromDate: null, toDate: null, page: DEFAULT_PAGE })}
      />
    </div>
  );
};

export default AdminDailyClosingHistoryPage;
