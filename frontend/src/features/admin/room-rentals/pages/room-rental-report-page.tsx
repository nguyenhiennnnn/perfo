import { useState } from "react";
import { ErrorState } from "@/components/common/error-state";
import { PageHeader } from "@/components/common/page-header";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { Field, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { getBusinessToday } from "@/lib/business-time";
import { formatCurrency, formatDate, formatPercent } from "@/lib/formatters";
import { RoomRentalReportStat } from "../components/room-rental-report-stat";
import { RoomRentalStatusBadge } from "../components/room-rental-status-badge";
import { useAdminRoomRentalReport } from "../hooks/use-admin-room-rentals";
import { addDays } from "../utils/room-rental-dates";
import {
  ROOM_RENTAL_REPORT_DEFAULT_DAYS,
  ROOM_RENTAL_REPORT_MAX_DAYS,
} from "../utils/room-rental-admin-labels";

const dayCount = (from: string, to: string) =>
  Math.round((Date.parse(to) - Date.parse(from)) / 86_400_000) + 1;

const AdminRoomRentalReportPage = () => {
  const today = getBusinessToday();
  const [to, setTo] = useState(today);
  const [from, setFrom] = useState(addDays(today, -(ROOM_RENTAL_REPORT_DEFAULT_DAYS - 1)));

  const days = dayCount(from, to);
  const rangeError =
    !from || !to
      ? "Chọn đủ khoảng ngày."
      : days < 1
        ? "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu."
        : days > ROOM_RENTAL_REPORT_MAX_DAYS
          ? `Khoảng ngày không được vượt quá ${ROOM_RENTAL_REPORT_MAX_DAYS} ngày.`
          : null;

  const report = useAdminRoomRentalReport({ from, to }, rangeError === null);
  const data = report.data;

  return (
    <div>
      <PageHeader
        title="Báo cáo thuê phòng"
        description="Số yêu cầu, tỷ lệ chốt và doanh thu thuê phòng. Doanh thu được ghi nhận vào ngày sự kiện diễn ra."
      />

      <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:max-w-lg">
        <Field>
          <FieldLabel htmlFor="room-rental-report-from">Từ ngày (ngày gửi yêu cầu)</FieldLabel>
          <Input
            id="room-rental-report-from"
            type="date"
            value={from}
            max={to || undefined}
            onChange={(event) => setFrom(event.target.value)}
          />
        </Field>
        <Field>
          <FieldLabel htmlFor="room-rental-report-to">Đến ngày</FieldLabel>
          <Input
            id="room-rental-report-to"
            type="date"
            value={to}
            min={from || undefined}
            onChange={(event) => setTo(event.target.value)}
          />
        </Field>
      </div>
      {rangeError && <p className="mb-4 text-sm text-destructive">{rangeError}</p>}

      {rangeError ? null : report.isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }, (_, index) => (
            <CardSkeleton key={index} className="h-28" />
          ))}
        </div>
      ) : report.isError && !data ? (
        <ErrorState error={report.error} onRetry={() => void report.refetch()} />
      ) : (
        data && (
          <div className="space-y-6">
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <RoomRentalReportStat label="Số yêu cầu" value={data.totalRequests} />
              <RoomRentalReportStat
                label="Đã báo giá"
                value={data.quotedRequests}
                hint="Yêu cầu đã được gửi ít nhất một báo giá"
              />
              <RoomRentalReportStat
                label="Tỷ lệ chốt"
                value={formatPercent(data.closeRate)}
                hint={`${data.closedRequests} yêu cầu đã đặt cọc, thanh toán đủ hoặc hoàn tất trên ${data.quotedRequests} yêu cầu đã báo giá`}
              />
              <RoomRentalReportStat
                label="Doanh thu đã ghi nhận"
                value={formatCurrency(data.recognizedRevenue)}
                hint={`${formatDate(data.from)} – ${formatDate(data.to)}`}
              />
              <RoomRentalReportStat
                label="Đã chốt, chờ ghi nhận"
                value={formatCurrency(data.pendingRevenue)}
                hint="Giá trị báo giá của các yêu cầu đã chốt, chưa đến ngày sự kiện"
              />
              <RoomRentalReportStat label="Yêu cầu đã hủy" value={data.cancelledRequests} />
            </div>

            <div className="grid items-start gap-6 lg:grid-cols-2">
              <section className="cut-md bg-card p-4">
                <h2 className="t-h3 mb-2 text-foreground">Yêu cầu theo trạng thái</h2>
                {data.byStatus.length === 0 ? (
                  <p className="text-sm text-muted-foreground">Chưa có yêu cầu trong khoảng này.</p>
                ) : (
                  <ul className="divide-y divide-border">
                    {data.byStatus.map((row) => (
                      <li key={row.status} className="flex items-center justify-between gap-3 py-2">
                        <RoomRentalStatusBadge status={row.status} />
                        <span className="tabular-nums text-sm font-medium text-foreground">
                          {row.count}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section className="cut-md bg-card p-4">
                <h2 className="t-h3 mb-2 text-foreground">Doanh thu theo ngày sự kiện</h2>
                {data.daily.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    Chưa có doanh thu thuê phòng được ghi nhận trong khoảng này.
                  </p>
                ) : (
                  <ul className="divide-y divide-border">
                    {data.daily.map((row) => (
                      <li
                        key={row.businessDate}
                        className="flex items-center justify-between gap-3 py-2 text-sm"
                      >
                        <span className="tabular-nums text-foreground">
                          {formatDate(row.businessDate)}
                          <span className="ml-2 text-muted-foreground">{row.events} sự kiện</span>
                        </span>
                        <span className="font-medium tabular-nums text-foreground">
                          {formatCurrency(row.revenue)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            </div>
          </div>
        )
      )}
    </div>
  );
};

export default AdminRoomRentalReportPage;
