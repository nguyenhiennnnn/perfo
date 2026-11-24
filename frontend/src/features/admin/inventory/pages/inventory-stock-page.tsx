import { Warehouse } from "lucide-react";
import { useNavigate } from "react-router";

import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { useAdminInventoryItemParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";

import { LowStockAlerts } from "../components/low-stock-alerts";
import { createStockLevelColumns } from "../components/stock-level-columns";
import { useAdminStockLevels } from "../hooks/use-admin-inventory";

const LOW_STOCK_OPTIONS = [{ value: "true", label: "Sắp hết hoặc hết hàng" }];

const AdminInventoryStockPage = () => {
  const navigate = useNavigate();
  const [params, setParams] = useAdminInventoryItemParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const stock = useAdminStockLevels({
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
    search: params.search || undefined,
    lowStockOnly: params.lowStock === "true" ? true : undefined,
  });

  const columns = createStockLevelColumns((level) =>
    navigate(ROUTES.admin.inventoryLedgerByItem(level.inventoryItemId)),
  );

  return (
    <div>
      <PageHeader
        title="Tồn kho"
        description="Tồn sổ sách, phần đang giữ chỗ cho đơn chờ thanh toán và tồn khả dụng của từng sản phẩm kho."
      />

      <LowStockAlerts />

      <DataTable
        columns={columns}
        data={stock.data?.items ?? []}
        isLoading={stock.isLoading}
        isError={stock.isError}
        error={stock.error}
        onRetry={() => void stock.refetch()}
        isRefreshing={stock.isPlaceholderData}
        totalItems={stock.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo mã hoặc tên sản phẩm kho..."
        hasActiveFilters={Boolean(params.search) || params.lowStock !== null}
        onClearFilters={() => setParams({ search: "", lowStock: null, page: DEFAULT_PAGE })}
        toolbarFilters={
          <EnumSelectFilter
            value={params.lowStock}
            onChange={(value) => setParams({ lowStock: value, page: DEFAULT_PAGE })}
            options={LOW_STOCK_OPTIONS}
            placeholder="Tình trạng tồn"
            allLabel="Tất cả tình trạng"
          />
        }
        empty={{
          icon: Warehouse,
          title: "Chưa có sản phẩm kho",
          description: "Thêm sản phẩm kho ở mục Sản phẩm kho để theo dõi tồn.",
        }}
      />
    </div>
  );
};

export default AdminInventoryStockPage;
