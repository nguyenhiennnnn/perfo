"use client";

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Ban } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { QueryState } from "@/components/common/query-state";
import { EmptyState } from "@/components/common/empty-state";
import { cn } from "@/lib/utils";
import {
  formatCurrency,
  formatDate,
  formatNumber,
  formatPercent,
} from "@/lib/formatters";
import type { DashboardCancellationGroupDto } from "@/features/admin/dashboard/types";
import { useDashboardCancellations } from "../hooks/use-dashboard";
import { useDashboardRange } from "../utils/dashboard-range";
import {
  formatOptionalPercent,
  getCancelReasonLabel,
  getCancelledByLabel,
  getTimeToShowtimeLabel,
} from "../utils/analytics-labels";

const chartConfig = {
  cancelledOrders: { label: "Đơn hủy", color: "var(--chart-3)" },
} satisfies ChartConfig;

const CHART_HEIGHT_CLASS = "h-56";
const AXIS_LABEL_MAX_LENGTH = 12;

const truncate = (text: string) =>
  text.length > AXIS_LABEL_MAX_LENGTH ? `${text.slice(0, AXIS_LABEL_MAX_LENGTH - 1)}…` : text;

const Metric = ({ value, label }: { value: string; label: string }) => (
  <div>
    <p className="t-numeral text-4xl text-foreground">{value}</p>
    <p className="mt-1 text-xs text-muted-foreground">{label}</p>
  </div>
);

interface GroupChartProps {
  title: string;
  items: DashboardCancellationGroupDto[];
  getLabel: (key: string) => string;
}

const GroupChart = ({ title, items, getLabel }: GroupChartProps) => {
  const rows = items.map((item) => ({
    name: getLabel(item.key),
    cancelledOrders: item.cancelledOrders,
    sharePercent: item.sharePercent,
    refundedAmount: item.refundedAmount,
  }));

  return (
    <div className="space-y-2">
      <p className="text-sm font-medium text-foreground">{title}</p>
      <ChartContainer config={chartConfig} className={cn(CHART_HEIGHT_CLASS, "w-full")}>
        <BarChart accessibilityLayer data={rows}>
          <CartesianGrid vertical={false} />
          <XAxis
            dataKey="name"
            tickLine={false}
            axisLine={false}
            tickMargin={10}
            interval={0}
            tickFormatter={truncate}
          />
          <YAxis tickLine={false} axisLine={false} tickMargin={8} width={32} allowDecimals={false} />
          <ChartTooltip
            content={
              <ChartTooltipContent
                formatter={(_value, _name, item) => {
                  const row = item.payload as (typeof rows)[number];
                  return (
                    <div className="flex w-full flex-col gap-1">
                      <div className="flex items-center justify-between gap-4">
                        <span className="text-muted-foreground">Đơn hủy</span>
                        <span className="font-medium tabular-nums text-foreground">
                          {formatNumber(row.cancelledOrders)} (
                          {formatOptionalPercent(row.sharePercent, formatPercent)})
                        </span>
                      </div>
                      <div className="flex items-center justify-between gap-4">
                        <span className="text-muted-foreground">Đã hoàn</span>
                        <span className="font-medium tabular-nums text-foreground">
                          {formatCurrency(row.refundedAmount)}
                        </span>
                      </div>
                    </div>
                  );
                }}
              />
            }
          />
          <Bar dataKey="cancelledOrders" fill="var(--color-cancelledOrders)" radius={0} />
        </BarChart>
      </ChartContainer>
    </div>
  );
};

export const CancellationsSection = () => {
  const { from, to } = useDashboardRange();
  const query = useDashboardCancellations({ from, to });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Tỷ lệ hủy</CardTitle>
        <CardDescription>
          Đơn đã thanh toán bị hủy, theo lý do, người hủy và thời điểm hủy so với giờ chiếu (
          {formatDate(from)} – {formatDate(to)}).
        </CardDescription>
      </CardHeader>
      <CardContent>
        <QueryState
          query={query}
          skeleton={<CardSkeleton className={CHART_HEIGHT_CLASS} />}
          isEmpty={(data) => data.total.paidOrders === 0 && data.total.cancelledOrders === 0}
          empty={
            <EmptyState
              icon={Ban}
              title="Chưa có dữ liệu hủy"
              description="Không có đơn thanh toán nào trong khoảng ngày đã chọn."
            />
          }
          errorTitle="Không tải được tỷ lệ hủy"
        >
          {(data) => (
            <div
              className={cn(
                "space-y-4 transition-opacity",
                query.isPlaceholderData && "opacity-60",
              )}
            >
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <Metric
                  value={formatOptionalPercent(data.total.cancellationRatePercent, formatPercent)}
                  label="Tỷ lệ hủy"
                />
                <Metric
                  value={`${formatNumber(data.total.cancelledOrders)} / ${formatNumber(data.total.paidOrders)}`}
                  label="Đơn hủy / đơn đã thanh toán"
                />
                <Metric
                  value={formatCurrency(data.total.refundedAmount)}
                  label="Tổng tiền đã hoàn"
                />
              </div>

              <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
                <GroupChart title="Theo lý do" items={data.byReason} getLabel={getCancelReasonLabel} />
                <GroupChart
                  title="Theo người hủy"
                  items={data.byCancelledBy}
                  getLabel={getCancelledByLabel}
                />
                <GroupChart
                  title="Hủy trước giờ chiếu"
                  items={data.byTimeToShowtime}
                  getLabel={getTimeToShowtimeLabel}
                />
              </div>

              <div className="space-y-2">
                <p className="text-sm font-medium text-foreground">Theo phim</p>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Phim</TableHead>
                      <TableHead className="text-right">Đơn thanh toán</TableHead>
                      <TableHead className="text-right">Đơn hủy</TableHead>
                      <TableHead className="text-right">Tỷ lệ hủy</TableHead>
                      <TableHead className="text-right">Đã hoàn</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {data.byMovie.map((item) => (
                      <TableRow key={item.movieId ?? "none"}>
                        <TableCell className="font-medium">
                          {item.movieTitle ?? "Không gắn phim"}
                        </TableCell>
                        <TableCell className="text-right tabular-nums">
                          {formatNumber(item.paidOrders)}
                        </TableCell>
                        <TableCell className="text-right tabular-nums">
                          {formatNumber(item.cancelledOrders)}
                        </TableCell>
                        <TableCell className="text-right tabular-nums">
                          {formatOptionalPercent(item.cancellationRatePercent, formatPercent)}
                        </TableCell>
                        <TableCell className="text-right tabular-nums">
                          {formatCurrency(item.refundedAmount)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>
          )}
        </QueryState>
      </CardContent>
    </Card>
  );
};
