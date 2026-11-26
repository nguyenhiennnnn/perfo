import { BadgeDollarSign, Plus } from "lucide-react";
import { DataTable } from "@/components/common/data-table/data-table";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE } from "@/config/default-constants";
import { useAdminListParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { RoomRentalRateForm } from "../components/forms/room-rental-rate-form";
import { createRoomRentalRateColumns } from "../components/room-rental-rate-columns";
import {
  useAdminDeleteRoomRentalRate,
  useAdminRoomRentalRates,
} from "../hooks/use-admin-room-rental-rates";
import type { RoomRentalRateDto } from "../types";

const AdminRoomRentalRatesPage = () => {
  const [params, setParams] = useAdminListParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const rates = useAdminRoomRentalRates({
    search: params.search || undefined,
    page: params.page,
    pageSize: params.pageSize,
  });
  const deleteRate = useAdminDeleteRoomRentalRate();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () =>
    openDialog({
      title: "Thêm bảng giá thuê phòng",
      description: "Giá thuê theo phòng, khung giờ và thời lượng; phí theo đầu người nếu có.",
      content: <RoomRentalRateForm onSuccess={closeDialog} />,
    });

  const handleEdit = (rate: RoomRentalRateDto) =>
    openDialog({
      title: "Cập nhật bảng giá thuê phòng",
      content: <RoomRentalRateForm rate={rate} onSuccess={closeDialog} />,
    });

  const handleDelete = (rate: RoomRentalRateDto) =>
    openAlertDialog({
      title: `Xóa bảng giá "${rate.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteRate.mutateAsync(rate.roomRentalRateId);
      },
    });

  const columns = createRoomRentalRateColumns({ onEdit: handleEdit, onDelete: handleDelete });

  return (
    <div>
      <PageHeader
        title="Bảng giá thuê phòng"
        description="Giá thuê theo phòng, khung giờ, thời lượng dùng để tính báo giá. Không áp dụng cho giá vé."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Thêm bảng giá</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={rates.data?.items ?? []}
        isLoading={rates.isLoading}
        isError={rates.isError}
        error={rates.error}
        onRetry={() => void rates.refetch()}
        isRefreshing={rates.isPlaceholderData}
        totalItems={rates.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên bảng giá..."
        empty={{
          icon: BadgeDollarSign,
          title: "Chưa có bảng giá thuê phòng",
          description: "Thêm bảng giá để có thể soạn báo giá cho yêu cầu thuê phòng.",
          actionLabel: "Thêm bảng giá",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminRoomRentalRatesPage;
