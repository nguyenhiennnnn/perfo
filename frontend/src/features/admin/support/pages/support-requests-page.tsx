import { LifeBuoy } from "lucide-react";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { FilterBar, FilterSearch } from "@/components/common/filter/filter-bar";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useAdminSupportRequestParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";
import { useAuthUser } from "@/stores/auth-store";
import { createSupportRequestColumns } from "../components/support-request-columns";
import { useAdminSupportRequests } from "../hooks/use-admin-support";
import { SUPPORT_ASSIGNEE_FILTER_OPTIONS, SUPPORT_STATUS_TABS } from "../utils/support-labels";

const columns = createSupportRequestColumns();

const AdminSupportRequestsPage = () => {
  const [params, setParams] = useAdminSupportRequestParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const user = useAuthUser();
  const debouncedSearch = useDebouncedValue(params.search);

  const requests = useAdminSupportRequests(
    stripNulls({
      status: params.status,
      assigneeId: params.assignee === "mine" ? user?.userId : undefined,
      unassigned: params.assignee === "unassigned" ? true : undefined,
      search: debouncedSearch || undefined,
      page: params.page,
      pageSize: params.pageSize,
    }),
  );

  const hasActiveFilters = Boolean(params.search) || params.assignee !== "all";

  return (
    <div>
      <PageHeader
        title="Yêu cầu hỗ trợ"
        description="Tiếp nhận, gán người phụ trách và trao đổi với khách về các sự cố liên quan đến đơn hàng."
      />

      <div className="mb-4 flex flex-wrap gap-2" role="group" aria-label="Lọc theo trạng thái yêu cầu">
        {SUPPORT_STATUS_TABS.map((tab) => (
          <Button
            key={tab.label}
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
          placeholder="Mã đơn, tiêu đề, tên khách..."
          value={params.search}
          onChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        />
        <EnumSelectFilter
          value={params.assignee === "all" ? null : params.assignee}
          onChange={(value) =>
            setParams({ assignee: (value ?? "all") as "all" | "mine" | "unassigned", page: DEFAULT_PAGE })
          }
          options={SUPPORT_ASSIGNEE_FILTER_OPTIONS}
          placeholder="Người phụ trách"
          allLabel="Tất cả người phụ trách"
        />
      </FilterBar>

      <DataTable
        columns={columns}
        data={requests.data?.items ?? []}
        isLoading={requests.isLoading}
        isError={requests.isError}
        error={requests.error}
        onRetry={() => void requests.refetch()}
        isRefreshing={requests.isPlaceholderData}
        totalItems={requests.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: LifeBuoy,
          title: "Chưa có yêu cầu hỗ trợ",
          description: "Yêu cầu của khách và yêu cầu tự tạo từ đối soát sẽ xuất hiện tại đây.",
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() => setParams({ search: "", assignee: "all", page: DEFAULT_PAGE })}
      />
    </div>
  );
};

export default AdminSupportRequestsPage;
