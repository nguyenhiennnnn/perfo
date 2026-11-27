import { Repeat } from "lucide-react";

import { PageHeader } from "@/components/common/page-header";
import { QueryState } from "@/components/common/query-state";
import { EmptyState } from "@/components/common/empty-state";
import { SelectField } from "@/components/common/select-field";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { useAdminRetentionParams } from "@/hooks/use-query-params";
import { formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";

import { RetentionCohortTable } from "../components/retention-cohort-table";
import { useRetentionReport } from "../hooks/use-retention-report";
import { RETENTION_RANGE_OPTIONS, normalizeRetentionMonths } from "../utils/retention-report";

const AdminRetentionReportPage = () => {
  const [params, setParams] = useAdminRetentionParams();
  const months = normalizeRetentionMonths(params.months);
  const query = useRetentionReport(months);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Báo cáo giữ chân khách"
        description="Tỷ lệ khách đăng ký cùng một tháng quay lại đặt vé ở tháng thứ nhất, thứ hai và thứ ba sau tháng đăng ký."
      />

      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div className="w-full sm:w-56">
          <SelectField
            value={String(months)}
            onChange={(value) => {
              if (value) void setParams({ months: Number(value) });
            }}
            options={RETENTION_RANGE_OPTIONS}
            getOptionValue={(option) => option.value}
            getOptionLabel={(option) => option.label}
          />
        </div>
        {query.data?.updatedAt && (
          <p className="text-xs text-muted-foreground">
            Cập nhật lần cuối: {formatDateTime(query.data.updatedAt)}
          </p>
        )}
      </div>

      <QueryState
        query={query}
        skeleton={<CardSkeleton className="h-72" />}
        isEmpty={(data) => data.cohorts.length === 0}
        empty={
          <EmptyState
            icon={Repeat}
            title="Chưa có dữ liệu giữ chân"
            description="Báo cáo được cập nhật theo ngày khi có khách đăng ký và đặt vé."
          />
        }
        errorTitle="Không tải được báo cáo giữ chân"
      >
        {(data) => (
          <div className={cn("space-y-3 transition-opacity", query.isPlaceholderData && "opacity-60")}>
            <RetentionCohortTable cohorts={data.cohorts} />
            <p className="text-xs text-muted-foreground">
              Tháng chưa kết thúc được đánh dấu &quot;Tạm tính&quot; và có thể thay đổi. Dấu — nghĩa
              là chưa có số liệu.
            </p>
          </div>
        )}
      </QueryState>
    </div>
  );
};

export default AdminRetentionReportPage;
