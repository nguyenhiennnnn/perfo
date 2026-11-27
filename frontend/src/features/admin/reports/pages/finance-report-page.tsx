import { FileSpreadsheet } from "lucide-react";
import { PageHeader } from "@/components/common/page-header";
import { ExportButton, type ExportFormat } from "@/components/common/export-button";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { QueryState } from "@/components/common/query-state";
import { EmptyState } from "@/components/common/empty-state";
import { ENDPOINTS } from "@/api/endpoints";
import { useAdminFinanceReportParams, type FinanceReportPeriod } from "@/hooks/use-query-params";
import { cn } from "@/lib/utils";
import { formatDate } from "@/lib/formatters";
import { useFinanceReport } from "../hooks/use-reports";
import { FinancePeriodFilter } from "../components/finance-period-filter";
import { ComboPreorderSummary, COMBO_PREORDER_BLOCK_KEY } from "../components/combo-preorder-summary";
import { SupportRequestSummary, SUPPORT_REQUEST_BLOCK_KEY } from "../components/support-request-summary";
import { MembershipSummary, MEMBERSHIP_SUMMARY_BLOCK_KEY } from "../components/membership-summary";
import { RoomQualityAlertPanel } from "../components/room-quality-alert-panel";
import { FinanceBlockTable } from "../components/finance-block-table";
import { resolveFinanceRange, shiftFinanceAnchor } from "../utils/finance-period";

const FINANCE_EXPORT_FORMATS: ExportFormat[] = ["xlsx", "pdf"];

const AdminFinanceReportPage = () => {
  const [params, setParams] = useAdminFinanceReportParams();
  const range = resolveFinanceRange(params.period, params.date, params.from, params.to);
  const hasValidRange = Boolean(range.from && range.to && range.from <= range.to);

  const query = useFinanceReport({ from: range.from, to: range.to }, hasValidRange);

  const handlePeriodChange = (period: FinanceReportPeriod) =>
    void setParams(
      period === "custom"
        ? { period, from: range.from, to: range.to }
        : { period, date: range.anchor, from: null, to: null },
    );

  return (
    <div className="space-y-6">
      <PageHeader
        title="Báo cáo tài chính"
        description={`Doanh thu ghi nhận, hoàn tiền, giảm giá và lợi nhuận (${formatDate(range.from)} – ${formatDate(range.to)}).`}
        action={
          <ExportButton
            endpoint={ENDPOINTS.admin.reports.financeExport}
            jobEndpoint={ENDPOINTS.admin.reports.financeExportJobs}
            params={{ from: range.from, to: range.to }}
            formats={FINANCE_EXPORT_FORMATS}
            fallbackFilename="finance-report"
            disabled={!hasValidRange}
          />
        }
      />

      <RoomQualityAlertPanel />

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
          skeleton={
            <div className="space-y-4">
              <CardSkeleton className="h-56" />
              <CardSkeleton className="h-56" />
            </div>
          }
          isEmpty={(data) => data.blocks.every((b) => b.rows.length === 0)}
          empty={
            <EmptyState
              icon={FileSpreadsheet}
              title="Chưa có dữ liệu tài chính"
              description="Không có số liệu nào trong kỳ đã chọn."
            />
          }
          errorTitle="Không tải được báo cáo tài chính"
        >
          {(data) => (
            <div
              className={cn("space-y-4 transition-opacity", query.isPlaceholderData && "opacity-60")}
            >
              {data.blocks
                .filter((block) => block.rows.length > 0)
                .map((block) => (
                  <FinanceBlockTable key={block.key} block={block}>
                    {block.key === COMBO_PREORDER_BLOCK_KEY && <ComboPreorderSummary block={block} />}
                    {block.key === SUPPORT_REQUEST_BLOCK_KEY && <SupportRequestSummary block={block} />}
                    {block.key === MEMBERSHIP_SUMMARY_BLOCK_KEY && <MembershipSummary block={block} />}
                  </FinanceBlockTable>
                ))}
            </div>
          )}
        </QueryState>
      )}
    </div>
  );
};

export default AdminFinanceReportPage;
