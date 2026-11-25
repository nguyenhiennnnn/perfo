import { ArrowLeft, BarChart3, History, RotateCcw } from "lucide-react";
import { Link, useParams } from "react-router";

import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { EmptyState } from "@/components/common/empty-state";
import { NotFoundState } from "@/components/common/not-found-state";
import { PageHeader } from "@/components/common/page-header";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { SkeletonList } from "@/components/common/skeleton-list";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ROUTES } from "@/config/routes";
import { useAdminPromotionDetailReportParams } from "@/hooks/use-query-params";
import { formatCurrency, formatNumber } from "@/lib/formatters";
import { getDefaultReportRange } from "@/features/admin/reports/utils/default-range";

import {
  PromotionBreakdownChart,
  type BreakdownRow,
} from "../components/promotion-breakdown-chart";
import { PromotionStatusBadge } from "../components/promotion-status-badge";
import { UsageProgress } from "../components/usage-progress";
import { useAdminPromotionReport } from "../hooks/use-admin-promotion-reports";
import type { PromotionReportBreakdownItemDto, PromotionReportDto } from "../types";
import {
  CHANNEL_LABELS,
  TIME_SLOT_INFO,
  derivePromotionStatus,
  getPromotionTypeLabel,
  getTimeSlotLabel,
} from "../utils/promotion-admin-labels";
import { FilterBar } from "@/components/common/filter/filter-bar";

const CHANNEL_ORDER = ["Online", "Counter"];
const TIME_SLOT_ORDER = ["Morning", "Afternoon", "Evening", "Night"];

const toRows = (
  items: PromotionReportBreakdownItemDto[],
  order: string[],
  getLabel: (key: string) => string,
  getAxisLabel: (key: string) => string,
): BreakdownRow[] => {
  const byKey = new Map(items.map((item) => [item.key, item]));
  const keys = [...order, ...items.map((item) => item.key).filter((key) => !order.includes(key))];
  return keys.map((key) => {
    const item = byKey.get(key);
    return {
      key,
      label: getLabel(key),
      axisLabel: getAxisLabel(key),
      redeemedCount: item?.redeemedCount ?? 0,
      totalDiscount: item?.totalDiscount ?? 0,
      orderRevenue: item?.orderRevenue ?? 0,
    };
  });
};

const ReportSkeleton = () => (
  <div aria-busy="true" className="space-y-4">
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      <SkeletonList count={8} className="h-24" />
    </div>
    <CardSkeleton className="h-32" />
    <CardSkeleton className="h-72" />
  </div>
);

const ReportContent = ({ report, promotionId }: { report: PromotionReportDto; promotionId: number }) => {
  const { usage, limits } = report;

  const metrics = [
    { label: "Lượt dùng", value: formatNumber(usage.redeemedCount) },
    { label: "Đang giữ chỗ", value: formatNumber(usage.activeHoldCount) },
    { label: "Đã nhả lại", value: formatNumber(usage.releasedCount) },
    { label: "Số đơn hàng", value: formatNumber(usage.orderCount) },
    { label: "Số khách hàng", value: formatNumber(usage.customerCount) },
    { label: "Tổng tiền giảm", value: formatCurrency(usage.totalDiscount) },
    { label: "Doanh thu đơn dùng mã", value: formatCurrency(usage.orderRevenue) },
    { label: "Giảm trung bình mỗi đơn", value: formatCurrency(usage.averageDiscountPerOrder) },
  ];

  const hasNoUsage =
    usage.redeemedCount === 0 && usage.activeHoldCount === 0 && usage.releasedCount === 0;

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {metrics.map((metric) => (
          <Card key={metric.label} className="gap-2 py-4">
            <CardContent className="px-4">
              <p className="text-xs font-medium text-muted-foreground">{metric.label}</p>
              <p className="mt-1.5 truncate text-xl font-semibold tabular-nums text-foreground">
                {metric.value}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>
      <p className="-mt-3 text-xs text-muted-foreground">
        Lượt đang giữ chỗ tính toàn thời gian, không theo khoảng ngày.
      </p>

      <Card>
        <CardHeader>
          <CardTitle>Giới hạn sử dụng</CardTitle>
          <CardDescription>Tính toàn thời gian, gồm cả lượt đang giữ chỗ.</CardDescription>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-6 sm:grid-cols-2">
          <div className="space-y-1">
            <UsageProgress
              label="Lượt dùng"
              used={limits.consumedCount}
              limit={limits.totalUsageLimit}
              ratio={limits.usageRatio}
            />
            {limits.remainingUsage != null && (
              <p className="text-xs text-muted-foreground">
                Còn lại {formatNumber(limits.remainingUsage)} lượt
              </p>
            )}
          </div>
          <div className="space-y-1">
            <UsageProgress
              label="Ngân sách"
              used={limits.consumedBudget}
              limit={limits.budgetAmount}
              ratio={limits.budgetRatio}
              format={formatCurrency}
            />
            {limits.remainingBudget != null && (
              <p className="text-xs text-muted-foreground">
                Còn lại {formatCurrency(limits.remainingBudget)}
              </p>
            )}
          </div>
        </CardContent>
      </Card>

      {hasNoUsage ? (
        <EmptyState
          icon={BarChart3}
          title="Chưa có lượt dùng"
          description="Chưa có lượt dùng nào của khuyến mãi này trong khoảng ngày đã chọn."
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
          <PromotionBreakdownChart
            title="Theo kênh bán"
            description="Lượt dùng, tiền giảm và doanh thu theo kênh."
            color="var(--chart-1)"
            rows={toRows(
              report.byChannel,
              CHANNEL_ORDER,
              (key) => CHANNEL_LABELS[key] ?? key,
              (key) => CHANNEL_LABELS[key] ?? key,
            )}
          />
          <PromotionBreakdownChart
            title="Theo khung giờ"
            description="Lượt dùng, tiền giảm và doanh thu theo khung giờ đặt."
            color="var(--chart-3)"
            rows={toRows(
              report.byTimeSlot,
              TIME_SLOT_ORDER,
              getTimeSlotLabel,
              (key) => TIME_SLOT_INFO[key]?.label ?? key,
            )}
          />
        </div>
      )}

      <div className="flex justify-end">
        <Button variant="outline" render={<Link to={ROUTES.admin.promotionRedemptions(promotionId)} />}>
          <History className="size-4" />
          <span>Xem lịch sử dùng</span>
        </Button>
      </div>
    </div>
  );
};

const AdminPromotionReportDetailPage = () => {
  const { id } = useParams();
  const promotionId = Number(id) || 0;

  const [params, setParams] = useAdminPromotionDetailReportParams();
  const defaultRange = getDefaultReportRange();
  const fromDate = params.fromDate ?? defaultRange.fromDate;
  const toDate = params.toDate ?? defaultRange.toDate;

  const report = useAdminPromotionReport(promotionId, { fromDate, toDate });
  const promotion = report.data?.promotion;

  return (
    <div className="space-y-6">
      <PageHeader
        title={promotion ? `Báo cáo · ${promotion.code}` : "Báo cáo khuyến mãi"}
        description={
          promotion
            ? `${promotion.name} · ${getPromotionTypeLabel(promotion.type)}`
            : "Hiệu quả sử dụng của khuyến mãi."
        }
        action={
          <div className="flex flex-wrap items-center gap-2">
            {promotion && (
              <PromotionStatusBadge
                status={derivePromotionStatus(promotion, {
                  consumedCount: report.data?.limits.consumedCount ?? 0,
                  consumedBudget: report.data?.limits.consumedBudget ?? 0,
                })}
              />
            )}
            <Button variant="outline" render={<Link to={ROUTES.admin.promotionReports} />}>
              <ArrowLeft className="size-4" />
              <span>Báo cáo khuyến mãi</span>
            </Button>
          </div>
        }
      />

      <FilterBar>
        <DateFilterPicker
          value={fromDate}
          onChange={(value) => setParams({ fromDate: value })}
          placeholder="Từ ngày"
        />
        <DateFilterPicker
          value={toDate}
          onChange={(value) => setParams({ toDate: value })}
          placeholder="Đến ngày"
        />
        <Button
          variant="outline"
          className="h-10"
          onClick={() => setParams({ fromDate: null, toDate: null })}
        >
          <RotateCcw className="size-4" />
          <span>Đặt lại</span>
        </Button>
      </FilterBar>

      <QueryState
        query={report}
        skeleton={<ReportSkeleton />}
        notFound={<NotFoundState entity="khuyến mãi" />}
        errorTitle="Không tải được báo cáo khuyến mãi"
      >
        {(data) => <ReportContent report={data} promotionId={promotionId} />}
      </QueryState>
    </div>
  );
};

export default AdminPromotionReportDetailPage;
