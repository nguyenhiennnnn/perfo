"use client";

import { CalendarX, RotateCcw } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { DateFilterPicker } from "@/components/common/filter/date-filter-picker";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { QueryState } from "@/components/common/query-state";
import { EmptyState } from "@/components/common/empty-state";
import { useAdminScheduleAnalysisParams } from "@/hooks/use-query-params";
import { getDefaultReportRange } from "@/features/admin/reports/utils/default-range";
import { cn } from "@/lib/utils";
import { formatCurrency, formatDate, formatNumber, formatPercent } from "@/lib/formatters";
import { useScheduleReport } from "../hooks/use-schedule-analysis";
import { getTimeSlotLabel } from "../utils/labels";

const DEFAULT_RANGE_DAYS = 30;

export const ScheduleReportSection = () => {
  const [params, setParams] = useAdminScheduleAnalysisParams();
  const defaultRange = getDefaultReportRange(DEFAULT_RANGE_DAYS);
  const from = params.from ?? defaultRange.fromDate;
  const to = params.to ?? defaultRange.toDate;

  const query = useScheduleReport({ from, to });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Hiệu quả theo phim, phòng và khung giờ</CardTitle>
        <CardDescription>
          Số suất, lấp đầy trung bình và doanh thu ({formatDate(from)} – {formatDate(to)}).
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex flex-wrap items-center gap-3">
          <DateFilterPicker
            value={from}
            onChange={(value) => void setParams({ from: value })}
            placeholder="Từ ngày"
          />
          <DateFilterPicker
            value={to}
            onChange={(value) => void setParams({ to: value })}
            placeholder="Đến ngày"
          />
          <Button
            variant="outline"
            className="h-10"
            onClick={() => void setParams({ from: null, to: null })}
          >
            <RotateCcw className="size-4" />
            <span>Đặt lại</span>
          </Button>
        </div>

        <QueryState
          query={query}
          skeleton={<CardSkeleton className="h-72" />}
          isEmpty={(data) => data.items.length === 0}
          empty={
            <EmptyState
              icon={CalendarX}
              title="Chưa có dữ liệu lịch chiếu"
              description="Không có suất chiếu nào trong khoảng ngày đã chọn."
            />
          }
          errorTitle="Không tải được phân tích lịch chiếu"
        >
          {(data) => (
            <div className={cn("transition-opacity", query.isPlaceholderData && "opacity-60")}>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Phim</TableHead>
                    <TableHead>Phòng</TableHead>
                    <TableHead>Khung giờ</TableHead>
                    <TableHead className="text-right">Số suất</TableHead>
                    <TableHead className="text-right">Lấp đầy TB</TableHead>
                    <TableHead className="text-right">Doanh thu / suất</TableHead>
                    <TableHead className="text-right">Doanh thu / ghế / giờ</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((row) => (
                    <TableRow key={`${row.movieId}-${row.roomId}-${row.timeSlot}`}>
                      <TableCell className="font-medium">
                        {row.movieTitle ?? `Phim #${row.movieId}`}
                      </TableCell>
                      <TableCell>{row.roomName ?? `Phòng #${row.roomId}`}</TableCell>
                      <TableCell>{getTimeSlotLabel(row.timeSlot)}</TableCell>
                      <TableCell className="text-right tabular-nums">
                        {formatNumber(row.showtimeCount)}
                      </TableCell>
                      <TableCell className="text-right tabular-nums">
                        {row.occupancyPercent == null ? "—" : formatPercent(row.occupancyPercent)}
                      </TableCell>
                      <TableCell className="text-right tabular-nums">
                        {formatCurrency(row.revenuePerShowtime)}
                      </TableCell>
                      <TableCell className="text-right tabular-nums">
                        {row.revenuePerSeatHour == null
                          ? "—"
                          : formatCurrency(row.revenuePerSeatHour)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </QueryState>
      </CardContent>
    </Card>
  );
};
