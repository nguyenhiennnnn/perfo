import { ArrowLeft } from "lucide-react";
import { useParams } from "react-router";

import { Button } from "@/components/ui/button";
import { ButtonLink } from "@/components/common/button-link";
import { PageHeader } from "@/components/common/page-header";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDate, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { useAdminDailyClosingReportParams } from "@/hooks/use-query-params";

import { ClosingReportView } from "../components/closing-report-view";
import { DailyClosingSummary } from "../components/daily-closing-summary";
import { useAdminDailyClosingReport } from "../hooks/use-admin-daily-closings";
import type { DailyClosingReportMode } from "../types";

const MODE_OPTIONS: { value: DailyClosingReportMode; label: string }[] = [
  { value: "OriginalDate", label: "Theo ngày gốc" },
  { value: "PostingDate", label: "Theo ngày ghi sổ" },
];

const AdminDailyClosingReportPage = () => {
  const { date = "" } = useParams();
  const [params, setParams] = useAdminDailyClosingReportParams();
  const report = useAdminDailyClosingReport(date, params.mode);

  return (
    <div className="space-y-4">
      <PageHeader
        title={`Báo cáo chốt ngày ${formatDate(date)}`}
        description="So sánh số liệu chốt với các bút toán điều chỉnh phát sinh sau."
        action={
          <ButtonLink to={ROUTES.admin.dailyClosingHistory} variant="outline">
            <ArrowLeft className="size-4" />
            <span>Lịch sử chốt ngày</span>
          </ButtonLink>
        }
      />

      <div role="group" aria-label="Chế độ xem" className="flex flex-wrap gap-2">
        {MODE_OPTIONS.map((option) => (
          <Button
            key={option.value}
            variant={params.mode === option.value ? "default" : "outline"}
            size="sm"
            aria-pressed={params.mode === option.value}
            onClick={() => void setParams({ mode: option.value })}
          >
            {option.label}
          </Button>
        ))}
      </div>

      <QueryState
        query={report}
        skeleton={
          <div className="space-y-4">
            <CardSkeleton className="h-64" />
            <CardSkeleton className="h-40" />
          </div>
        }
        errorTitle="Không tải được báo cáo chốt"
      >
        {(data) => (
          <div className="space-y-4">
            {data.closing ? (
              <div className="space-y-1 bg-success/10 p-4 text-sm text-foreground">
                <p className="font-medium">
                  Đã chốt lúc {formatDateTime(data.closing.closedAt)} bởi {data.closing.closedByName}
                </p>
                <p className="tabular-nums text-muted-foreground">
                  Tiền mặt theo sổ {formatCurrency(data.closing.expectedCash)}, thực đếm{" "}
                  {formatCurrency(data.closing.countedCash)}, chênh lệch{" "}
                  <span
                    className={cn(
                      "font-medium",
                      data.closing.variance === 0 ? "text-success" : "text-warning-text",
                    )}
                  >
                    {formatCurrency(data.closing.variance)}
                  </span>
                </p>
                {data.closing.note ? <p>Ghi chú: {data.closing.note}</p> : null}
              </div>
            ) : (
              <p className="bg-warning/10 p-3 text-sm text-foreground">
                Ngày này chưa chốt, số liệu bên dưới là báo cáo nháp.
              </p>
            )}
            <ClosingReportView report={data} isRefreshing={report.isPlaceholderData} />
            <DailyClosingSummary snapshot={data.snapshot} />
          </div>
        )}
      </QueryState>
    </div>
  );
};

export default AdminDailyClosingReportPage;
