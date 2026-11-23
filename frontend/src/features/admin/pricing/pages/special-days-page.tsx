import type { ColumnFiltersState } from "@tanstack/react-table";
import { CalendarHeart, Plus } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import type { EnumOption } from "@/lib/enum-options";
import { getBusinessToday } from "@/lib/business-time";
import { useDialogClose, useDialogOpen } from "@/stores/dialog-store";
import { useAlertDialogOpen } from "@/stores/alert-dialog-store";
import { useAdminSpecialDayParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { DEFAULT_PAGE, IRREVERSIBLE_ACTION_MESSAGE } from "@/config/default-constants";

import { useAdminDeleteSpecialDay, useAdminSpecialDays } from "../hooks/use-admin-special-days";
import { createSpecialDayColumns } from "../components/special-day-columns";
import { SpecialDayForm } from "../components/forms/special-day-form";
import type { SpecialDayDto } from "../types";

const YEARS_BEFORE = 1;
const YEARS_AFTER = 2;

const buildYearOptions = (): EnumOption[] => {
  const currentYear = Number(getBusinessToday().slice(0, 4));
  return Array.from({ length: YEARS_BEFORE + YEARS_AFTER + 1 }, (_, index) => {
    const year = currentYear - YEARS_BEFORE + index;
    return { value: String(year), label: `Năm ${year}` };
  });
};

const AdminSpecialDaysPage = () => {
  const [params, setParams] = useAdminSpecialDayParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const specialDays = useAdminSpecialDays({
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
    search: params.search || undefined,
    year: params.year ?? undefined,
  });
  const deleteSpecialDay = useAdminDeleteSpecialDay();

  const openDialog = useDialogOpen();
  const closeDialog = useDialogClose();
  const openAlertDialog = useAlertDialogOpen();

  const handleCreate = () => {
    openDialog({
      title: "Thêm ngày lễ hoặc sự kiện",
      description: "Khai báo khoảng ngày để dùng trong quy tắc giá ngày lễ.",
      content: <SpecialDayForm onSuccess={closeDialog} />,
    });
  };

  const handleEdit = (specialDay: SpecialDayDto) => {
    openDialog({
      title: "Cập nhật ngày lễ hoặc sự kiện",
      description: "Chỉnh sửa tên và khoảng ngày.",
      content: <SpecialDayForm specialDay={specialDay} onSuccess={closeDialog} />,
    });
  };

  const handleDelete = (specialDay: SpecialDayDto) => {
    openAlertDialog({
      title: `Xóa "${specialDay.name}"?`,
      description: IRREVERSIBLE_ACTION_MESSAGE,
      confirmText: "Xóa",
      variant: "destructive",
      onConfirm: async () => {
        await deleteSpecialDay.mutateAsync(specialDay.specialDayId);
      },
    });
  };

  const columns = createSpecialDayColumns({ onEdit: handleEdit, onDelete: handleDelete });

  const columnFilters: ColumnFiltersState = params.search
    ? [{ id: "name", value: params.search }]
    : [];

  const hasActiveFilters = Boolean(params.search) || params.year !== null;

  return (
    <div>
      <PageHeader
        title="Lịch ngày lễ"
        description="Danh sách ngày lễ và sự kiện đặc biệt dùng cho quy tắc giá."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Thêm ngày lễ</span>
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={specialDays.data?.items ?? []}
        isLoading={specialDays.isLoading}
        isError={specialDays.isError}
        error={specialDays.error}
        onRetry={() => void specialDays.refetch()}
        isRefreshing={specialDays.isPlaceholderData}
        totalItems={specialDays.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        columnFilters={columnFilters}
        searchValue={params.search}
        onSearchChange={(value) => setParams({ search: value, page: DEFAULT_PAGE })}
        searchPlaceholder="Tìm theo tên ngày lễ..."
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() => setParams({ search: "", year: null, page: DEFAULT_PAGE })}
        toolbarFilters={
          <EnumSelectFilter
            value={params.year === null ? null : String(params.year)}
            onChange={(value) =>
              setParams({ year: value === null ? null : Number(value), page: DEFAULT_PAGE })
            }
            options={buildYearOptions()}
            placeholder="Năm"
            allLabel="Tất cả các năm"
          />
        }
        empty={{
          icon: CalendarHeart,
          title: "Chưa có ngày lễ",
          description: "Thêm ngày lễ để áp dụng giá riêng cho dịp đặc biệt.",
          actionLabel: "Thêm ngày lễ",
          onAction: handleCreate,
        }}
      />
    </div>
  );
};

export default AdminSpecialDaysPage;
