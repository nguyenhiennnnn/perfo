import { Package, Plus } from "lucide-react";
import { useNavigate } from "react-router";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { useAdminInventoryItemParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";

import { InventoryItemForm } from "../components/forms/inventory-item-form";
import { createInventoryItemColumns } from "../components/inventory-item-columns";
import { useAdminDeleteInventoryItem, useAdminInventoryItems } from "../hooks/use-admin-inventory";
import type { InventoryItemDto } from "../types";

const ACTIVE_OPTIONS = [
  { value: "true", label: "Đang sử dụng" },
  { value: "false", label: "Ngừng sử dụng" },
];
const LOW_STOCK_OPTIONS = [{ value: "true", label: "Sắp hết hoặc hết hàng" }];

const AdminInventoryItemsPage = () => {
  const navigate = useNavigate();
  const [params, setParams] = useAdminInventoryItemParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const items = useAdminInventoryItems({
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
    search: params.search || undefined,
    isActive: params.isActive === null ? undefined : params.isActive === "true",
    lowStockOnly: params.lowStock === "true" ? true : undefined,
  });
  const deleteItem = useAdminDeleteInventoryItem();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () => {
    openDialog({
      title: "Thêm sản phẩm kho",
      description: "Khai báo nguyên liệu hoặc hàng hóa bắp nước để theo dõi tồn.",
      content: <InventoryItemForm onSuccess={closeDialog} />,
    });
  };

  const handleEdit = (item: InventoryItemDto) => {
    openDialog({
      title: "Cập nhật sản phẩm kho",
      description: "Giá vốn được cập nhật tự động qua phiếu nhập kho.",
      content: <InventoryItemForm item={item} onSuccess={closeDialog} />,
    });
  };

  const handleDelete = (item: InventoryItemDto) => {
    openAlertDialog({
      title: `Xóa "${item.name}"?`,
      description: `${IRREVERSIBLE_ACTION_MESSAGE} Chỉ xóa được sản phẩm có tồn bằng 0 và không nằm trong công thức combo.`,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteItem.mutateAsync(item.inventoryItemId);
      },
    });
  };

  const columns = createInventoryItemColumns({
    onEdit: handleEdit,
    onDelete: handleDelete,
    onViewLedger: (item) => navigate(ROUTES.admin.inventoryLedgerByItem(item.inventoryItemId)),
  });

  const hasActiveFilters =
    Boolean(params.search) || params.isActive !== null || params.lowStock !== null;

  return (
    <div>
      <PageHeader
        title="Sản phẩm kho"
        description="Danh mục nguyên liệu và hàng hóa bắp nước, giá vốn hiện hành và ngưỡng cảnh báo."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Thêm sản phẩm kho</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={items.data?.items ?? []}
        isLoading={items.isLoading}
        isError={items.isError}
        error={items.error}
        onRetry={() => void items.refetch()}
        isRefreshing={items.isPlaceholderData}
        totalItems={items.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo mã hoặc tên sản phẩm kho..."
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          setParams({ search: "", isActive: null, lowStock: null, page: DEFAULT_PAGE })
        }
        toolbarFilters={
          <>
            <EnumSelectFilter
              value={params.isActive}
              onChange={(value) => setParams({ isActive: value, page: DEFAULT_PAGE })}
              options={ACTIVE_OPTIONS}
              placeholder="Trạng thái"
              allLabel="Tất cả trạng thái"
            />
            <EnumSelectFilter
              value={params.lowStock}
              onChange={(value) => setParams({ lowStock: value, page: DEFAULT_PAGE })}
              options={LOW_STOCK_OPTIONS}
              placeholder="Tình trạng tồn"
              allLabel="Tất cả tình trạng"
            />
          </>
        }
        empty={{
          icon: Package,
          title: "Chưa có sản phẩm kho",
          description: "Thêm sản phẩm kho rồi nhập kho để bắt đầu theo dõi tồn.",
          actionLabel: "Thêm sản phẩm kho",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminInventoryItemsPage;
