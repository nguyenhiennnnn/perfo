import { Landmark } from "lucide-react";
import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminReconciliationParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";
import { createSessionColumns } from "../components/session-columns";
import { NewReconciliationCard } from "../components/new-reconciliation-card";
import { ReconciliationItemsTable } from "../components/reconciliation-items-table";
import { useReconciliationSessions } from "../hooks/use-reconciliation";
import { GATEWAY_OPTIONS } from "../utils/reconciliation-labels";

const columns = createSessionColumns();

const AdminReconciliationPage = () => {
  const [params, setParams] = useAdminReconciliationParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const filter = stripNulls({
    gateway: params.gateway,
    fromDate: params.fromDate,
    toDate: params.toDate,
  });

  const sessions = useReconciliationSessions({
    ...filter,
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const hasActiveFilters =
    params.gateway !== null || params.fromDate !== null || params.toDate !== null;

  return (
    <div className="space-y-4">
      <PageHeader
        title="Đối soát cổng thanh toán"
        description="So khớp sao kê của cổng thanh toán với giao dịch trong hệ thống."
      />

      <NewReconciliationCard />

      <Tabs
        value={params.tab}
        onValueChange={(tab) =>
          void setParams({ tab: tab as "sessions" | "items", page: DEFAULT_PAGE })
        }
      >
        <TabsList>
          <TabsTrigger value="sessions">Phiên đối soát</TabsTrigger>
          <TabsTrigger value="items">Dòng bất thường</TabsTrigger>
        </TabsList>
      </Tabs>

      {params.tab === "items" ? (
        <ReconciliationItemsTable />
      ) : (
        <>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <EnumSelectFilter
              value={params.gateway}
              onChange={(gateway) => void setParams({ gateway, page: DEFAULT_PAGE })}
              options={GATEWAY_OPTIONS}
              placeholder="Cổng thanh toán"
              allLabel="Tất cả cổng"
            />
            <DateFilterPicker
              value={params.fromDate}
              onChange={(fromDate) => void setParams({ fromDate, page: DEFAULT_PAGE })}
              placeholder="Từ ngày"
            />
            <DateFilterPicker
              value={params.toDate}
              onChange={(toDate) => void setParams({ toDate, page: DEFAULT_PAGE })}
              placeholder="Đến ngày"
            />
          </div>

          <DataTable
            columns={columns}
            data={sessions.data?.items ?? []}
            isLoading={sessions.isLoading}
            isError={sessions.isError}
            error={sessions.error}
            onRetry={() => void sessions.refetch()}
            isRefreshing={sessions.isPlaceholderData}
            totalItems={sessions.data?.totalItems ?? 0}
            pagination={pagination}
            onPaginationChange={onPaginationChange}
            empty={{
              icon: Landmark,
              title: "Chưa có phiên đối soát",
              description: "Tải file sao kê hoặc chạy đối soát Stripe để tạo phiên đầu tiên.",
            }}
            hasActiveFilters={hasActiveFilters}
            onClearFilters={() =>
              void setParams({ gateway: null, fromDate: null, toDate: null, page: DEFAULT_PAGE })
            }
          />
        </>
      )}
    </div>
  );
};

export default AdminReconciliationPage;
