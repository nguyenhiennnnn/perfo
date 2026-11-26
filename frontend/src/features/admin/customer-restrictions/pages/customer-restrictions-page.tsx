import { UserX } from "lucide-react";

import { DataTable } from "@/components/common/data-table/data-table";
import { FilterBar, FilterSearch } from "@/components/common/filter/filter-bar";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useAdminCustomerRestrictionParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";

import { createCustomerRestrictionColumns } from "../components/customer-restriction-columns";
import { useCustomerRestrictionLift } from "../hooks/use-customer-restriction-lift";
import { useCustomerRestrictions } from "../hooks/use-customer-restrictions";
import { CustomerRestrictionStatusFilter } from "../types";

const STATUS_TABS: { value: CustomerRestrictionStatusFilter; label: string }[] = [
  { value: CustomerRestrictionStatusFilter.Active, label: "Đang hạn chế" },
  { value: CustomerRestrictionStatusFilter.Lifted, label: "Đã gỡ" },
  { value: CustomerRestrictionStatusFilter.Expired, label: "Hết hạn" },
  { value: CustomerRestrictionStatusFilter.All, label: "Tất cả" },
];

const AdminCustomerRestrictionsPage = () => {
  const [params, setParams] = useAdminCustomerRestrictionParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const liftRestriction = useCustomerRestrictionLift();

  const debouncedSearch = useDebouncedValue(params.search);
  const restrictions = useCustomerRestrictions(
    stripNulls({
      status: params.status === CustomerRestrictionStatusFilter.All ? undefined : params.status,
      search: debouncedSearch || undefined,
      page: params.page,
      pageSize: params.pageSize,
    }),
  );

  const columns = createCustomerRestrictionColumns({ onLift: liftRestriction });

  return (
    <div>
      <PageHeader
        title="Khách bị hạn chế"
        description="Theo dõi khách bị hạn chế giữ ghế do bỏ đơn liên tiếp, xem lý do, thời hạn và gỡ hạn chế khi cần."
      />

      <div
        className="mb-4 flex flex-wrap gap-2"
        role="group"
        aria-label="Lọc theo trạng thái hạn chế"
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
          placeholder="Tìm theo tên, email, số điện thoại..."
          value={params.search}
          onChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        />
      </FilterBar>

      <DataTable
        columns={columns}
        data={restrictions.data?.items ?? []}
        isLoading={restrictions.isLoading}
        isError={restrictions.isError}
        error={restrictions.error}
        onRetry={() => void restrictions.refetch()}
        isRefreshing={restrictions.isPlaceholderData}
        totalItems={restrictions.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: UserX,
          title:
            params.status === CustomerRestrictionStatusFilter.Active
              ? "Không có khách nào đang bị hạn chế"
              : "Chưa có khách bị hạn chế",
          description:
            "Khách xuất hiện ở đây khi có đơn giữ ghế hết hạn liên tiếp vượt ngưỡng cấu hình.",
        }}
        hasActiveFilters={Boolean(params.search)}
        onClearFilters={() => setParams({ search: "", page: DEFAULT_PAGE })}
      />
    </div>
  );
};

export default AdminCustomerRestrictionsPage;
