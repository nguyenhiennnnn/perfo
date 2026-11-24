import { Coins } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { QueryState } from "@/components/common/query-state";
import { LineSkeleton } from "@/components/common/skeleton-shapes";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminListParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

import { createValuationColumns } from "../components/valuation-columns";
import { useAdminInventoryValuation } from "../hooks/use-admin-inventory";

const AdminInventoryValuationPage = () => {
  const [params, setParams] = useAdminListParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const valuation = useAdminInventoryValuation();

  const keyword = params.search.trim().toLowerCase();
  const allItems = valuation.data?.items ?? [];
  const filtered = keyword
    ? allItems.filter(
        (item) =>
          item.name.toLowerCase().includes(keyword) || item.code.toLowerCase().includes(keyword),
      )
    : allItems;
  const start = pagination.pageIndex * pagination.pageSize;
  const pageItems = filtered.slice(start, start + pagination.pageSize);

  return (
    <div>
      <PageHeader
        title="Giá trị tồn"
        description="Giá trị tồn kho hiện tại theo giá vốn bình quân, sắp xếp từ giá trị cao đến thấp."
      />

      <QueryState
        query={valuation}
        skeleton={<LineSkeleton className="mb-6 h-14 w-64" />}
        errorTitle="Không tải được báo cáo giá trị tồn"
        errorClassName="mb-6"
      >
        {(data) => (
          <div className="mb-6">
            <p className="text-sm text-muted-foreground">Tổng giá trị tồn kho</p>
            <p className="t-h1 tabular-nums text-foreground">{formatCurrency(data.totalValue)}</p>
            <p className="mt-1 text-xs text-muted-foreground">
              Tính lúc {formatDateTime(data.generatedAt)}, chỉ gồm sản phẩm còn tồn.
            </p>
          </div>
        )}
      </QueryState>

      <DataTable
        columns={createValuationColumns(valuation.data?.totalValue ?? 0)}
        data={pageItems}
        isLoading={valuation.isLoading}
        isError={valuation.isError}
        error={valuation.error}
        onRetry={() => void valuation.refetch()}
        totalItems={filtered.length}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo mã hoặc tên sản phẩm kho..."
        empty={{
          icon: Coins,
          title: "Chưa có hàng tồn",
          description: "Nhập kho để có giá trị tồn.",
        }}
      />
    </div>
  );
};

export default AdminInventoryValuationPage;
