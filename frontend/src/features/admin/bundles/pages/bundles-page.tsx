import { Package, Plus } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminBundleParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE, STATUS_LABEL } from "@/config/default-constants";

import { useAdminBundles, useAdminDeleteBundle } from "../hooks/use-admin-bundles";
import { createBundleColumns } from "../components/bundle-columns";
import { BundleForm } from "../components/forms/bundle-form";
import type { AdminBundleDto } from "../types";
import { ACTIVE_FILTER_OPTIONS, parseActiveFilter } from "../utils/bundle-admin-labels";

const AdminBundlesPage = () => {
  const [params, setParams] = useAdminBundleParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const bundles = useAdminBundles({
    search: useDebouncedValue(params.search) || undefined,
    isActive: parseActiveFilter(params.isActive),
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const deleteBundle = useAdminDeleteBundle();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () => {
    openDialog({
      title: "Tạo gói sản phẩm",
      description: "Chọn thành phần, giá gói, điều kiện áp dụng và hạn mức mỗi ngày.",
      content: <BundleForm onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleEdit = (bundle: AdminBundleDto) => {
    openDialog({
      title: "Cập nhật gói sản phẩm",
      description: "Mọi thay đổi được ghi vào nhật ký thao tác.",
      content: <BundleForm bundle={bundle} onSuccess={closeDialog} />,
      size: "lg",
    });
  };

  const handleDelete = (bundle: AdminBundleDto) => {
    openAlertDialog({
      title: `Xóa gói "${bundle.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteBundle.mutateAsync(bundle.bundleId);
      },
    });
  };

  const columns = createBundleColumns({ onEdit: handleEdit, onDelete: handleDelete });

  return (
    <div>
      <PageHeader
        title="Gói sản phẩm"
        description="Các gói vé và combo bán theo giá gói thấp hơn mua lẻ, kèm điều kiện áp dụng và hạn mức mỗi ngày."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Tạo gói</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={bundles.data?.items ?? []}
        isLoading={bundles.isLoading}
        isError={bundles.isError}
        error={bundles.error}
        onRetry={() => void bundles.refetch()}
        isRefreshing={bundles.isPlaceholderData}
        totalItems={bundles.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên gói..."
        hasActiveFilters={Boolean(params.search) || params.isActive !== null}
        onClearFilters={() => setParams({ search: "", isActive: null, page: DEFAULT_PAGE })}
        toolbarFilters={
          <EnumSelectFilter
            value={params.isActive}
            onChange={(isActive) => setParams({ isActive, page: DEFAULT_PAGE })}
            options={ACTIVE_FILTER_OPTIONS}
            placeholder={STATUS_LABEL}
            allLabel="Tất cả trạng thái"
          />
        }
        empty={{
          icon: Package,
          title: "Chưa có gói sản phẩm",
          description: "Tạo gói vé kèm combo để khách mua tiết kiệm hơn mua lẻ.",
          actionLabel: "Tạo gói",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminBundlesPage;
