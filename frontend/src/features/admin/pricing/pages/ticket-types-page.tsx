import { useMemo } from "react";
import type { ColumnFiltersState } from "@tanstack/react-table";
import { Plus, Tags } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminListParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { useAdminAgeRatings } from "@/features/admin/catalog/hooks/use-admin-age-rating";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE, LOOKUP_PAGE_SIZE } from "@/config/default-constants";

import { useAdminDeleteTicketType, useAdminTicketTypes } from "../hooks/use-admin-ticket-types";
import { createTicketTypeColumns } from "../components/ticket-type-columns";
import { TicketTypeForm } from "../components/forms/ticket-type-form";
import type { TicketTypeDto } from "../types";

const AdminTicketTypesPage = () => {
  const [params, setParams] = useAdminListParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const ticketTypes = useAdminTicketTypes({
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
    search: params.search || undefined,
  });
  const ageRatings = useAdminAgeRatings({ pageSize: LOOKUP_PAGE_SIZE });
  const deleteTicketType = useAdminDeleteTicketType();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const ageRatingCodes = useMemo(
    () => new Map((ageRatings.data?.items ?? []).map((a) => [a.ageRatingId, a.code] as const)),
    [ageRatings.data],
  );

  const handleCreate = () => {
    openDialog({
      title: "Thêm loại vé mới",
      description: "Đặt tên, hệ số giá và điều kiện áp dụng cho loại vé.",
      content: <TicketTypeForm onSuccess={closeDialog} />,
    });
  };

  const handleEdit = (ticketType: TicketTypeDto) => {
    openDialog({
      title: "Cập nhật loại vé",
      description: "Chỉnh sửa hệ số giá và điều kiện áp dụng.",
      content: <TicketTypeForm ticketType={ticketType} onSuccess={closeDialog} />,
    });
  };

  const handleDelete = (ticketType: TicketTypeDto) => {
    openAlertDialog({
      title: `Xóa loại vé "${ticketType.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteTicketType.mutateAsync(ticketType.ticketTypeId);
      },
    });
  };

  const columns = createTicketTypeColumns({
    ageRatingCodes,
    onEdit: handleEdit,
    onDelete: handleDelete,
  });

  const columnFilters: ColumnFiltersState = params.search
    ? [{ id: "name", value: params.search }]
    : [];

  return (
    <div>
      <PageHeader
        title="Loại vé"
        description="Quản lý loại vé, hệ số giá và điều kiện áp dụng (thẻ, độ tuổi phim, khung giờ, ngày)."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Thêm loại vé</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={ticketTypes.data?.items ?? []}
        isLoading={ticketTypes.isLoading}
        isError={ticketTypes.isError}
        error={ticketTypes.error}
        onRetry={() => void ticketTypes.refetch()}
        isRefreshing={ticketTypes.isPlaceholderData}
        totalItems={ticketTypes.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        columnFilters={columnFilters}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên loại vé..."
        empty={{
          icon: Tags,
          title: "Chưa có loại vé",
          description: "Thêm loại vé để khách chọn khi đặt vé.",
          actionLabel: "Thêm loại vé",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminTicketTypesPage;
