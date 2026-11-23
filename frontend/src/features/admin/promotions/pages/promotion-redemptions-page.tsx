import { ArrowLeft, History } from "lucide-react";
import { Link, useParams } from "react-router";

import { ENDPOINTS } from "@/api/endpoints";
import { DataTable } from "@/components/common/data-table/data-table";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { EnumSelectFilter } from "@/components/common/filter/enum-select-filter";
import { ExportButton } from "@/components/common/export-button";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ALL_STATUSES_LABEL, DEFAULT_PAGE, STATUS_LABEL } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { useDebouncedValue } from "@/hooks/use-debounced-value";
import { useAdminPromotionRedemptionParams } from "@/hooks/use-query-params";
import { useTablePagination } from "@/hooks/use-table-pagination";
import { orderChannelOptions } from "@/lib/enum-options";
import { stripNulls } from "@/lib/query-params";

import { createRedemptionColumns } from "../components/redemption-columns";
import { useAdminPromotionDetail } from "../hooks/use-admin-promotions";
import { useAdminPromotionRedemptions } from "../hooks/use-admin-promotion-redemptions";
import { REDEMPTION_STATUS_OPTIONS } from "../utils/promotion-admin-labels";

const columns = createRedemptionColumns();

const AdminPromotionRedemptionsPage = () => {
  const { id } = useParams();
  const promotionId = Number(id) || 0;

  const [params, setParams] = useAdminPromotionRedemptionParams();
  const { pagination, onPaginationChange } = useTablePagination(params, setParams);

  const promotion = useAdminPromotionDetail(promotionId);

  const filter = stripNulls({
    search: useDebouncedValue(params.search) || null,
    status: params.status,
    channel: params.channel,
    fromDate: params.fromDate,
    toDate: params.toDate,
  });

  const redemptions = useAdminPromotionRedemptions(promotionId, {
    ...filter,
    page: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
  });

  const hasActiveFilters =
    Boolean(params.search) ||
    params.status !== null ||
    params.channel !== null ||
    params.fromDate !== null ||
    params.toDate !== null;

  const title = promotion.data
    ? `Lịch sử dùng · ${promotion.data.code}`
    : "Lịch sử dùng khuyến mãi";

  return (
    <div>
      <PageHeader
        title={title}
        description={promotion.data?.name ?? "Các lượt giữ chỗ, sử dụng và nhả lại của khuyến mãi."}
        action={
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" render={<Link to={ROUTES.admin.promotions} />}>
              <ArrowLeft className="size-4" />
              <span>Danh sách khuyến mãi</span>
            </Button>
            <ExportButton
              endpoint={ENDPOINTS.admin.promotions.redemptionsExport(promotionId)}
              jobEndpoint={ENDPOINTS.admin.promotions.redemptionsExportJobs(promotionId)}
              params={filter}
              fallbackFilename="promotion-redemptions"
              size="default"
              disabled={promotionId === 0}
            />
          </div>
        }
      />

      <div className="mb-4 grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-5">
        <Input
          placeholder="Tìm theo mã đơn, tên khách..."
          value={params.search}
          onChange={(e) => setParams({ search: e.target.value, page: DEFAULT_PAGE })}
          className="col-span-2 md:col-span-3 xl:col-span-1"
        />
        <EnumSelectFilter
          value={params.status}
          onChange={(status) => setParams({ status, page: DEFAULT_PAGE })}
          options={REDEMPTION_STATUS_OPTIONS}
          placeholder={STATUS_LABEL}
          allLabel={ALL_STATUSES_LABEL}
        />
        <EnumSelectFilter
          value={params.channel}
          onChange={(channel) => setParams({ channel, page: DEFAULT_PAGE })}
          options={orderChannelOptions}
          placeholder="Kênh bán"
          allLabel="Tất cả kênh"
        />
        <DateFilterPicker
          value={params.fromDate}
          onChange={(fromDate) => setParams({ fromDate, page: DEFAULT_PAGE })}
          placeholder="Từ ngày"
        />
        <DateFilterPicker
          value={params.toDate}
          onChange={(toDate) => setParams({ toDate, page: DEFAULT_PAGE })}
          placeholder="Đến ngày"
        />
      </div>

      <DataTable
        columns={columns}
        data={redemptions.data?.items ?? []}
        isLoading={redemptions.isLoading}
        isError={redemptions.isError}
        error={redemptions.error}
        onRetry={() => void redemptions.refetch()}
        isRefreshing={redemptions.isPlaceholderData}
        totalItems={redemptions.data?.totalItems ?? 0}
        pagination={pagination}
        onPaginationChange={onPaginationChange}
        empty={{
          icon: History,
          title: "Chưa có lượt dùng",
          description: "Lượt dùng hiện ở đây khi khách áp khuyến mãi này vào đơn hàng.",
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          setParams({
            search: "",
            status: null,
            channel: null,
            fromDate: null,
            toDate: null,
            page: DEFAULT_PAGE,
          })
        }
      />
    </div>
  );
};

export default AdminPromotionRedemptionsPage;
