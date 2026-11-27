import { Plus, Target } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminCustomerSegmentParams } from "@/hooks/use-query-params";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE, STATUS_LABEL } from "@/config/default-constants";
import { parseActiveFilter } from "@/features/admin/membership-plans/utils/membership-plan-admin-labels";

import {
  useAdminCustomerSegments,
  useAdminDeleteCustomerSegment,
  useSegmentLookups,
} from "../hooks/use-admin-customer-segments";
import { createCustomerSegmentColumns } from "../components/customer-segment-columns";
import { CustomerSegmentForm } from "../components/forms/customer-segment-form";
import type { CustomerSegmentDto } from "../types";
import { SEGMENT_ACTIVE_FILTER_OPTIONS } from "../utils/segment-condition-fields";

const AdminCustomerSegmentsPage = () => {
  const [params, setParams] = useAdminCustomerSegmentParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const segments = useAdminCustomerSegments({
    search: useDebouncedValue(params.search) || undefined,
    isActive: parseActiveFilter(params.isActive),
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });
  const lookups = useSegmentLookups();
  const deleteSegment = useAdminDeleteCustomerSegment();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () => {
    openDialog({
      title: "Tạo phân khúc tùy chỉnh",
      description: "Kết hợp các điều kiện VÀ, HOẶC trên chỉ số hoạt động, giá trị, sở thích và điểm.",
      content: <CustomerSegmentForm onSuccess={closeDialog} />,
      size: "xl",
    });
  };

  const handleEdit = (segment: CustomerSegmentDto) => {
    openDialog({
      title: "Cập nhật phân khúc",
      description: "Mọi thay đổi được ghi vào nhật ký thao tác.",
      content: <CustomerSegmentForm segment={segment} onSuccess={closeDialog} />,
      size: "xl",
    });
  };

  const handleDelete = (segment: CustomerSegmentDto) => {
    openAlertDialog({
      title: `Xóa phân khúc "${segment.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteSegment.mutateAsync(segment.segmentId);
      },
    });
  };

  const columns = createCustomerSegmentColumns({
    lookups,
    onEdit: handleEdit,
    onDelete: handleDelete,
  });

  return (
    <div>
      <PageHeader
        title="Phân khúc khách hàng"
        description="Số khách và số khách đủ điều kiện nhận thông tin (đã bật tiếp thị) của từng phân khúc, cập nhật định kỳ."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Tạo phân khúc</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={segments.data?.items ?? []}
        isLoading={segments.isLoading}
        isError={segments.isError}
        error={segments.error}
        onRetry={() => void segments.refetch()}
        isRefreshing={segments.isPlaceholderData}
        totalItems={segments.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên hoặc mô tả..."
        hasActiveFilters={Boolean(params.search) || params.isActive !== null}
        onClearFilters={() => setParams({ search: "", isActive: null, page: DEFAULT_PAGE })}
        toolbarFilters={
          <EnumSelectFilter
            value={params.isActive}
            onChange={(isActive) => setParams({ isActive, page: DEFAULT_PAGE })}
            options={SEGMENT_ACTIVE_FILTER_OPTIONS}
            placeholder={STATUS_LABEL}
            allLabel="Tất cả trạng thái"
          />
        }
        empty={{
          icon: Target,
          title: "Chưa có phân khúc khách hàng",
          description: "Tạo phân khúc để nhóm khách theo hành vi và chăm sóc có chủ đích.",
          actionLabel: "Tạo phân khúc",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminCustomerSegmentsPage;
