import { ClipboardCheck, Plus } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminStockSheetParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { StocktakeForm } from "../components/forms/stocktake-form";
import { createStocktakeColumns } from "../components/stock-sheet-columns";
import { StocktakeDetailView } from "../components/stock-sheet-detail-views";
import { StockSheetFilters } from "../components/stock-sheet-filters";
import { useAdminStocktakes } from "../hooks/use-admin-stock-sheets";

const AdminStocktakesPage = () => {
  const [params, setParams] = useAdminStockSheetParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const stocktakes = useAdminStocktakes(
    stripNulls({
      search: params.search || undefined,
      fromDate: params.fromDate,
      toDate: params.toDate,
      inventoryItemId: params.itemId,
      page: pagination.pageIndex + 1,
      pageSize: pagination.pageSize,
    }),
  );

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  const handleCreate = () => {
    openDialog({
      title: "Lập phiếu kiểm kê",
      description: "Nhập số đếm thực tế; chênh lệch với sổ sách được ghi thành điều chỉnh tồn.",
      size: "xl",
      content: <StocktakeForm onSuccess={closeDialog} />,
    });
  };

  const handleView = (id: number) => {
    openDialog({
      title: `Phiếu kiểm kê #${id}`,
      size: "lg",
      content: <StocktakeDetailView id={id} />,
    });
  };

  const hasActiveFilters =
    Boolean(params.search) || params.fromDate !== null || params.toDate !== null || params.itemId !== null;

  return (
    <div>
      <PageHeader
        title="Kiểm kê"
        description="Phiếu kiểm kê, số đếm thực tế và chênh lệch so với sổ sách."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Lập phiếu kiểm kê</span>
          </Button>
        }
      />

      <DataTable
        columns={createStocktakeColumns(handleView)}
        data={stocktakes.data?.items ?? []}
        isLoading={stocktakes.isLoading}
        isError={stocktakes.isError}
        error={stocktakes.error}
        onRetry={() => void stocktakes.refetch()}
        isRefreshing={stocktakes.isPlaceholderData}
        totalItems={stocktakes.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo ghi chú..."
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          setParams({ search: "", fromDate: null, toDate: null, itemId: null, page: DEFAULT_PAGE })
        }
        toolbarFilters={
          <StockSheetFilters
            values={{ fromDate: params.fromDate, toDate: params.toDate, itemId: params.itemId, reason: null }}
            onChange={(patch) => setParams({ ...patch, page: DEFAULT_PAGE })}
          />
        }
        empty={{
          icon: ClipboardCheck,
          title: "Chưa có phiếu kiểm kê",
          description: "Lập phiếu kiểm kê để đối chiếu tồn thực tế với sổ sách.",
          actionLabel: "Lập phiếu kiểm kê",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminStocktakesPage;
