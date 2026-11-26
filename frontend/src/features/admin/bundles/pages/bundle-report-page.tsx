import { FileSpreadsheet } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { QueryState } from "@/components/common/query-state";
import { EmptyState } from "@/components/common/empty-state";
import { useAdminFinanceReportParams, type FinanceReportPeriod } from "@/hooks/use-query-params";
import { cn } from "@/lib/utils";
import { formatDate } from "@/lib/formatters";
import { useFinanceReport } from "@/features/admin/reports/hooks/use-reports";
import { FinancePeriodFilter } from "@/features/admin/reports/components/finance-period-filter";
import { FinanceBlockTable } from "@/features/admin/reports/components/finance-block-table";
import {
  resolveFinanceRange,
  shiftFinanceAnchor,
} from "@/features/admin/reports/utils/finance-period";

import { BundleReportSummary } from "../components/bundle-report-summary";
import { BUNDLE_REPORT_BLOCK_KEY } from "../utils/bundle-admin-labels";

const AdminBundleReportPage = () => {
  const [params, setParams] = useAdminFinanceReportParams();
  const range = resolveFinanceRange(params.period, params.date, params.from, params.to);
  const hasValidRange = Boolean(range.from && range.to && range.from <= range.to);

  const query = useFinanceReport(
    { from: range.from, to: range.to, blocks: BUNDLE_REPORT_BLOCK_KEY },
    hasValidRange,
  );

  const handlePeriodChange = (period: FinanceReportPeriod) =>
    void setParams(
      period === "custom"
        ? { period, from: range.from, to: range.to }
        : { period, date: range.anchor, from: null, to: null },
    );

  return (
    <div className="space-y-6">
      <PageHeader
        title="Báo cáo gói sản phẩm"
        description={`Số gói bán, doanh thu gói, so sánh với mua lẻ và tỷ lệ chuyển đổi (${formatDate(range.from)} – ${formatDate(range.to)}).`}
      />

      <FinancePeriodFilter
        period={params.period}
        anchor={range.anchor}
        from={range.from}
        to={range.to}
        onPeriodChange={handlePeriodChange}
        onAnchorChange={(value) => void setParams({ date: value })}
        onShift={(direction) =>
          void setParams({ date: shiftFinanceAnchor(params.period, range.anchor, direction) })
        }
        onFromChange={(value) => void setParams({ from: value })}
        onToChange={(value) => void setParams({ to: value })}
        onReset={() => void setParams({ period: null, date: null, from: null, to: null })}
      />

      {!hasValidRange ? (
        <EmptyState
          icon={FileSpreadsheet}
          title="Khoảng ngày không hợp lệ"
          description="Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc."
        />
      ) : (
        <QueryState
          query={query}
          skeleton={<CardSkeleton className="h-72" />}
          isEmpty={(data) => data.blocks.every((block) => block.rows.length === 0)}
          empty={
            <EmptyState
              icon={FileSpreadsheet}
              title="Chưa có dữ liệu gói sản phẩm"
              description="Không có gói nào được bán hoặc xem trong kỳ đã chọn."
            />
          }
          errorTitle="Không tải được báo cáo gói sản phẩm"
        >
          {(data) => (
            <div
              className={cn("space-y-4 transition-opacity", query.isPlaceholderData && "opacity-60")}
            >
              {data.blocks
                .filter((block) => block.key === BUNDLE_REPORT_BLOCK_KEY && block.rows.length > 0)
                .map((block) => (
                  <FinanceBlockTable key={block.key} block={block}>
                    <BundleReportSummary block={block} />
                  </FinanceBlockTable>
                ))}
            </div>
          )}
        </QueryState>
      )}
    </div>
  );
};

export default AdminBundleReportPage;
