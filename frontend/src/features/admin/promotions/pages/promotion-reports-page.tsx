import { BadgePercent, RotateCcw } from "lucide-react";

import { DataTable } from "@/components/common/data-table/data-table";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ALL_STATUSES_LABEL, DEFAULT_PAGE, STATUS_LABEL } from "@/config/default-constants";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useAdminPromotionReportParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { getDefaultReportRange } from "@/features/admin/reports/utils/default-range";

import { createPromotionReportColumns } from "../components/promotion-report-columns";
import { PromotionReportSummaryCards } from "../components/promotion-report-summary-cards";
import { useAdminPromotionReports } from "../hooks/use-admin-promotion-reports";
import {
  ACTIVE_FILTER_OPTIONS,
  PROMOTION_TYPE_OPTIONS,
  parseActiveFilter,
} from "../utils/promotion-admin-labels";

const typeFilterOptions = PROMOTION_TYPE_OPTIONS.map((o) => ({ value: o.value, label: o.label }));
const columns = createPromotionReportColumns();

const AdminPromotionReportsPage = () => {
  const [params, setParams] = useAdminPromotionReportParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const defaultRange = getDefaultReportRange();
  const fromDate = params.fromDate ?? defaultRange.fromDate;
  const toDate = params.toDate ?? defaultRange.toDate;

  const reports = useAdminPromotionReports({
    search: useDebouncedValue(params.search) || undefined,
    type: params.type ?? undefined,
    isActive: parseActiveFilter(params.isActive),
    fromDate,
    toDate,
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const handleReset = () =>
    setParams({
      search: "",
      type: null,
      isActive: null,
      fromDate: null,
      toDate: null,
      page: DEFAULT_PAGE,
    });

  const items = reports.data?.items;

  return (
    <div className="space-y-6">
      <PageHeader
        title="Báo cáo khuyến mãi"
        description="Lượt dùng, tiền giảm, doanh thu và mức sử dụng so với giới hạn của từng khuyến mãi."
      />

      <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
        <Input
          placeholder="Tìm theo mã hoặc tên khuyến mãi..."
          value={params.search}
          onChange={(e) => setParams({ search: e.target.value, page: DEFAULT_PAGE })}
          className="col-span-2 md:col-span-3 xl:col-span-2"
        />
        <EnumSelectFilter
          value={params.type}
          onChange={(type) => setParams({ type, page: DEFAULT_PAGE })}
          options={typeFilterOptions}
          placeholder="Loại khuyến mãi"
          allLabel="Tất cả loại"
        />
        <EnumSelectFilter
          value={params.isActive}
          onChange={(isActive) => setParams({ isActive, page: DEFAULT_PAGE })}
          options={ACTIVE_FILTER_OPTIONS}
          placeholder={STATUS_LABEL}
          allLabel={ALL_STATUSES_LABEL}
        />
        <DateFilterPicker
          value={fromDate}
          onChange={(value) => setParams({ fromDate: value, page: DEFAULT_PAGE })}
          placeholder="Từ ngày"
        />
        <DateFilterPicker
          value={toDate}
          onChange={(value) => setParams({ toDate: value, page: DEFAULT_PAGE })}
          placeholder="Đến ngày"
        />
        <Button variant="outline" onClick={handleReset} className="h-10">
          <RotateCcw className="size-4" />
          <span>Đặt lại</span>
        </Button>
      </div>

      <PromotionReportSummaryCards query={reports} />

      <DataTable
        columns={columns}
        data={items?.items ?? []}
        isLoading={reports.isLoading}
        isError={reports.isError}
        error={reports.error}
        onRetry={() => void reports.refetch()}
        isRefreshing={reports.isPlaceholderData}
        totalItems={items?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: BadgePercent,
          title: "Chưa có dữ liệu khuyến mãi",
          description: "Chưa có khuyến mãi nào khớp bộ lọc và khoảng ngày đã chọn.",
        }}
        hasActiveFilters={
          Boolean(params.search) ||
          params.type !== null ||
          params.isActive !== null ||
          params.fromDate !== null ||
          params.toDate !== null
        }
        onClearFilters={handleReset}
      />
    </div>
  );
};

export default AdminPromotionReportsPage;
