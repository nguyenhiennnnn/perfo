import { PackagePlus, Plus } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminStockSheetParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { StockReceiptForm } from "../components/forms/stock-receipt-form";
import { createReceiptColumns } from "../components/stock-sheet-columns";
import { StockReceiptDetailView } from "../components/stock-sheet-detail-views";
import { StockSheetFilters } from "../components/stock-sheet-filters";
import { useAdminStockReceipts } from "../hooks/use-admin-stock-sheets";

const AdminStockReceiptsPage = () => {
  const [params, setParams] = useAdminStockSheetParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const receipts = useAdminStockReceipts(
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
      title: "Lập phiếu nhập kho",
      description: "Nhập hàng từ nhà cung cấp; giá vốn bình quân của sản phẩm được cập nhật theo đơn giá nhập.",
      size: "xl",
      content: <StockReceiptForm onSuccess={closeDialog} />,
    });
  };

  const handleView = (id: number) => {
    openDialog({
      title: `Phiếu nhập kho #${id}`,
      size: "lg",
      content: <StockReceiptDetailView id={id} />,
    });
  };

  const hasActiveFilters =
    Boolean(params.search) || params.fromDate !== null || params.toDate !== null || params.itemId !== null;

  return (
    <div>
      <PageHeader
        title="Nhập kho"
        description="Phiếu nhập kho từ nhà cung cấp, số lượng và đơn giá nhập."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Lập phiếu nhập</span>
          </Button>
        }
      />

      <DataTable
        columns={createReceiptColumns(handleView)}
        data={receipts.data?.items ?? []}
        isLoading={receipts.isLoading}
        isError={receipts.isError}
        error={receipts.error}
        onRetry={() => void receipts.refetch()}
        isRefreshing={receipts.isPlaceholderData}
        totalItems={receipts.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo nhà cung cấp hoặc ghi chú..."
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
          icon: PackagePlus,
          title: "Chưa có phiếu nhập kho",
          description: "Lập phiếu nhập để tăng tồn và cập nhật giá vốn.",
          actionLabel: "Lập phiếu nhập",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminStockReceiptsPage;
