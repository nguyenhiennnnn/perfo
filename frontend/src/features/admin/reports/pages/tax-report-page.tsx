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
import { FinanceBlockTable } from "../components/finance-block-table";
import { resolveFinanceRange, shiftFinanceAnchor } from "../utils/finance-period";

const TAX_EXPORT_FORMATS: ExportFormat[] = ["xlsx", "pdf"];
const TAX_BLOCK_KEY = "vat";

const AdminTaxReportPage = () => {
  const [params, setParams] = useAdminFinanceReportParams();
  const range = resolveFinanceRange(params.period, params.date, params.from, params.to);
  const hasValidRange = Boolean(range.from && range.to && range.from <= range.to);

  const query = useFinanceReport(
    { from: range.from, to: range.to, blocks: TAX_BLOCK_KEY },
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
        title="Báo cáo thuế"
        description={`Doanh thu trước thuế và thuế GTGT phải nộp theo loại dòng hàng (${formatDate(range.from)} – ${formatDate(range.to)}).`}
        action={
          <ExportButton
            endpoint={ENDPOINTS.admin.reports.financeExport}
            jobEndpoint={ENDPOINTS.admin.reports.financeExportJobs}
            params={{ from: range.from, to: range.to }}
            formats={TAX_EXPORT_FORMATS}
            fallbackFilename="tax-report"
            disabled={!hasValidRange}
          />
        }
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
              title="Chưa có dữ liệu thuế"
              description="Không có dòng hàng nào ghi nhận doanh thu trong kỳ đã chọn."
            />
          }
          errorTitle="Không tải được báo cáo thuế"
        >
          {(data) => (
            <div
              className={cn("space-y-4 transition-opacity", query.isPlaceholderData && "opacity-60")}
            >
              {data.blocks
                .filter((block) => block.key === TAX_BLOCK_KEY && block.rows.length > 0)
                .map((block) => (
                  <FinanceBlockTable key={block.key} block={block} />
                ))}
            </div>
          )}
        </QueryState>
      )}
    </div>
  );
};

export default AdminTaxReportPage;
