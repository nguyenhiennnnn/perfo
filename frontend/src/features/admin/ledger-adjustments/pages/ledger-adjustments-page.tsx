import { Plus, Scale } from "lucide-react";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/common/page-header";
import { DataTable } from "@/components/common/data-table/data-table";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { stripNulls } from "@/lib/query-params";
import { DEFAULT_PAGE } from "@/config/default-constants";
import { useDialogOpen } from "@/stores/dialog-store";
import { useAdminLedgerAdjustmentParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";

import { useAdminLedgerAdjustments } from "../hooks/use-admin-ledger-adjustments";
import { createLedgerAdjustmentColumns } from "../components/ledger-adjustment-columns";
import { LedgerAdjustmentFormDialogContent } from "../components/ledger-adjustment-form-dialog";
import { ledgerAdjustmentTypeOptions } from "../utils/labels";
import { FilterBar } from "@/components/common/filter/filter-bar";

const columns = createLedgerAdjustmentColumns();

const AdminLedgerAdjustmentsPage = () => {
  const [params, setParams] = useAdminLedgerAdjustmentParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);
  const adjustments = useAdminLedgerAdjustments(stripNulls(params));
  const openDialog = useDialogOpen();

  const handleCreate = () =>
    openDialog({
      title: "Tạo bút toán điều chỉnh",
      description: "Điều chỉnh số liệu của một ngày đã chốt. Thao tác được ghi vào nhật ký.",
      content: <LedgerAdjustmentFormDialogContent />,
      size: "lg",
    });

  return (
    <div>
      <PageHeader
        title="Bút toán điều chỉnh"
        description="Các điều chỉnh vào ngày đang mở cho số liệu của những ngày đã chốt."
        action={
          <Button onClick={handleCreate}>
            <Plus className="size-4" />
            <span>Tạo bút toán</span>
          </Button>
        }
      />

      <FilterBar className="mb-4">
        <DateFilterPicker
          value={params.postingFrom}
          onChange={(postingFrom) => setParams({ postingFrom, page: DEFAULT_PAGE })}
          placeholder="Ghi sổ từ ngày"
        />
        <DateFilterPicker
          value={params.postingTo}
          onChange={(postingTo) => setParams({ postingTo, page: DEFAULT_PAGE })}
          placeholder="Ghi sổ đến ngày"
        />
        <DateFilterPicker
          value={params.originalDate}
          onChange={(originalDate) => setParams({ originalDate, page: DEFAULT_PAGE })}
          placeholder="Ngày gốc"
        />
        <EnumSelectFilter
          value={params.type}
          onChange={(type) => setParams({ type, page: DEFAULT_PAGE })}
          options={ledgerAdjustmentTypeOptions}
          placeholder="Loại bút toán"
          allLabel="Tất cả loại"
        />
      </FilterBar>

      <DataTable
        columns={columns}
        data={adjustments.data?.items ?? []}
        isLoading={adjustments.isLoading}
        isError={adjustments.isError}
        error={adjustments.error}
        onRetry={() => void adjustments.refetch()}
        isRefreshing={adjustments.isPlaceholderData}
        totalItems={adjustments.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: Scale,
          title: "Chưa có bút toán điều chỉnh",
          description:
            "Bút toán tự sinh khi có hoàn tiền hoặc hủy đơn của ngày đã chốt, hoặc do quản trị tạo thủ công.",
        }}
        hasActiveFilters={
          params.postingFrom !== null ||
          params.postingTo !== null ||
          params.originalDate !== null ||
          params.type !== null
        }
        onClearFilters={() =>
          setParams({
            postingFrom: null,
            postingTo: null,
            originalDate: null,
            type: null,
            page: DEFAULT_PAGE,
          })
        }
      />
    </div>
  );
};

export default AdminLedgerAdjustmentsPage;
