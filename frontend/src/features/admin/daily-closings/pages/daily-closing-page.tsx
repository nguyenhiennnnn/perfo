import { subDays } from "date-fns";
import { CheckCircle2 } from "lucide-react";

import { ButtonLink } from "@/components/common/button-link";
import { PageHeader } from "@/components/common/page-header";
import { QueryState } from "@/components/common/query-state";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { getBusinessToday, getBusinessTodayDate } from "@/lib/business-time";
import { formatCurrency, formatDate, formatDateTime, toDateInputValue } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { ROUTES } from "@/config/routes";
import { useAdminDailyClosingParams } from "@/hooks/use-query-params";
import { useAdminSettings } from "@/features/admin/settings/hooks/use-admin-settings";

import { DailyClosingClosePanel } from "../components/daily-closing-close-panel";
import { DailyClosingSummary } from "../components/daily-closing-summary";
import {
  useAdminDailyClosing,
  useAdminDailyClosingDraft,
} from "../hooks/use-admin-daily-closings";
import type { DailyClosingDto } from "../types";

const SummarySkeleton = () => (
  <div className="space-y-4">
    <CardSkeleton className="h-24" />
    <CardSkeleton className="h-56" />
    <CardSkeleton className="h-56" />
  </div>
);

const ClosedView = ({ data }: { data: DailyClosingDto }) => {
  const { closing, snapshot } = data;
  return (
    <div className="space-y-4">
      <div className="space-y-2 bg-success/10 p-4 text-sm text-foreground">
        <p className="flex items-center gap-2 font-medium">
          <CheckCircle2 className="size-5 text-success" />
          Ngày {formatDate(closing.businessDate)} đã chốt lúc {formatDateTime(closing.closedAt)} bởi{" "}
          {closing.closedByName}
        </p>
        <dl className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-1 sm:max-w-md">
          <dt className="text-muted-foreground">Tiền mặt theo sổ</dt>
          <dd className="text-right tabular-nums">{formatCurrency(closing.expectedCash)}</dd>
          <dt className="text-muted-foreground">Thực đếm</dt>
          <dd className="text-right tabular-nums">{formatCurrency(closing.countedCash)}</dd>
          <dt className="font-medium">Chênh lệch</dt>
          <dd
            className={cn(
              "text-right font-semibold tabular-nums",
              closing.variance === 0 ? "text-success" : "text-warning-text",
            )}
          >
            {formatCurrency(closing.variance)}
          </dd>
        </dl>
        {closing.note ? <p>Ghi chú: {closing.note}</p> : null}
        <p className="text-xs text-muted-foreground">
          Số liệu đã khóa. Điều chỉnh về sau được ghi bằng bút toán.
        </p>
        <ButtonLink
          to={ROUTES.admin.dailyClosingReport(closing.businessDate)}
          variant="outline"
          size="sm"
        >
          Xem theo ngày gốc và ngày ghi sổ
        </ButtonLink>
      </div>
      <DailyClosingSummary snapshot={snapshot} />
    </div>
  );
};

const DraftView = ({ date }: { date: string }) => {
  const draft = useAdminDailyClosingDraft(date);
  const settings = useAdminSettings();
  const dayNotEnded = date >= getBusinessToday();

  return (
    <QueryState
      query={draft}
      skeleton={<SummarySkeleton />}
      errorTitle="Không dựng được báo cáo nháp"
    >
      {(snapshot) => (
        <div className="space-y-4">
          <DailyClosingClosePanel
            key={date}
            date={date}
            snapshot={snapshot}
            dayNotEnded={dayNotEnded}
            allowOverride={settings.data?.dailyClosingAllowOverride === true}
          />
          <DailyClosingSummary snapshot={snapshot} />
        </div>
      )}
    </QueryState>
  );
};

const AdminDailyClosingPage = () => {
  const [params, setParams] = useAdminDailyClosingParams();
  const date = params.date ?? toDateInputValue(subDays(getBusinessTodayDate(), 1)) ?? "";
  const closing = useAdminDailyClosing(date);

  return (
    <div className="space-y-4">
      <PageHeader
        title="Chốt doanh thu ngày"
        description="Xem báo cáo nháp, nhập tiền mặt thực đếm và chốt số liệu chính thức của ngày kinh doanh."
        action={
          <DateFilterPicker
            value={date}
            onChange={(value) => void setParams({ date: value })}
            placeholder="Chọn ngày kinh doanh"
            className="w-full sm:w-52"
          />
        }
      />

      {date ? (
        <QueryState
          query={closing}
          skeleton={<SummarySkeleton />}
          notFound={<DraftView date={date} />}
          errorTitle="Không tải được dữ liệu chốt ngày"
        >
          {(data) => <ClosedView data={data} />}
        </QueryState>
      ) : null}
    </div>
  );
};

export default AdminDailyClosingPage;
