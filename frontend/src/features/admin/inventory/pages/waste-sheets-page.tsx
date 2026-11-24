import { PackageMinus, Plus } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useAdminStockSheetParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { stripNulls } from "@/lib/query-params";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { WasteForm } from "../components/forms/waste-form";
import { createWasteColumns } from "../components/stock-sheet-columns";
import { WasteSheetDetailView } from "../components/stock-sheet-detail-views";
import { StockSheetFilters } from "../components/stock-sheet-filters";
import { useAdminWasteSheets } from "../hooks/use-admin-stock-sheets";

const AdminWasteSheetsPage = () => {
  const [params, setParams] = useAdminStockSheetParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const sheets = useAdminWasteSheets(
    stripNulls({
      search: params.search || undefined,
      fromDate: params.fromDate,
      toDate: params.toDate,
      inventoryItemId: params.itemId,
      reason: params.reason,
      page: pagination.pageIndex + 1,
      pageSize: pagination.pageSize,
    }),
  );

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();

  const handleCreate = () => {
    openDialog({
      title: "Lập phiếu hao hụt",
      description: "Hủy hàng hết hạn, hư hỏng hoặc đổ vỡ; số lượng được trừ khỏi tồn.",
      size: "xl",
      content: <WasteForm onSuccess={closeDialog} />,
    });
  };

  const handleView = (id: number) => {
    openDialog({
      title: `Phiếu hao hụt #${id}`,
      size: "lg",
      content: <WasteSheetDetailView id={id} />,
    });
  };

  const hasActiveFilters =
    Boolean(params.search) ||
    params.fromDate !== null ||
    params.toDate !== null ||
    params.itemId !== null ||
    params.reason !== null;

  return (
    <div>
      <PageHeader
        title="Hao hụt"
        description="Phiếu hủy hàng hết hạn, hư hỏng, đổ vỡ và giá trị hao hụt theo giá vốn."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Lập phiếu hao hụt</span>
          </Button>
        }
      />

      <DataTable
        columns={createWasteColumns(handleView)}
        data={sheets.data?.items ?? []}
        isLoading={sheets.isLoading}
        isError={sheets.isError}
        error={sheets.error}
        onRetry={() => void sheets.refetch()}
        isRefreshing={sheets.isPlaceholderData}
        totalItems={sheets.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo ghi chú..."
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          setParams({
            search: "",
            fromDate: null,
            toDate: null,
            itemId: null,
            reason: null,
            page: DEFAULT_PAGE,
          })
        }
        toolbarFilters={
          <StockSheetFilters
            withReason
            values={{
              fromDate: params.fromDate,
              toDate: params.toDate,
              itemId: params.itemId,
              reason: params.reason,
            }}
            onChange={(patch) => setParams({ ...patch, page: DEFAULT_PAGE })}
          />
        }
        empty={{
          icon: PackageMinus,
          title: "Chưa có phiếu hao hụt",
          description: "Lập phiếu khi cần hủy hàng hết hạn, hỏng hoặc đổ vỡ.",
          actionLabel: "Lập phiếu hao hụt",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminWasteSheetsPage;
