"use client";

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Percent } from "lucide-react";
import type { UseQueryResult } from "@tanstack/react-query";
import {
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
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
import { formatDate, formatNumber, formatPercent } from "@/lib/formatters";
import type { OccupancyGroupBy } from "@/types/enum";
import type { DashboardOccupancyDto } from "@/features/admin/dashboard/types";
import {
  formatChangePoints,
  getChangeTextClass,
  getOccupancyGroupLabel,
} from "../utils/occupancy";

const chartConfig = {
  current: { label: "Kỳ này", color: "var(--chart-1)" },
  previous: { label: "Kỳ trước", color: "var(--chart-2)" },
} satisfies ChartConfig;

const CHART_HEIGHT_CLASS = "h-72";
const AXIS_LABEL_MAX_LENGTH = 14;

const formatOptionalPercent = (value: number | null | undefined) =>
  value == null ? "—" : formatPercent(value);

const truncate = (text: string) =>
  text.length > AXIS_LABEL_MAX_LENGTH ? `${text.slice(0, AXIS_LABEL_MAX_LENGTH - 1)}…` : text;

interface OccupancyChartProps {
  query: UseQueryResult<DashboardOccupancyDto>;
  groupBy: OccupancyGroupBy;
}

export const OccupancyChart = ({ query, groupBy }: OccupancyChartProps) => (
  <QueryState
    query={query}
    skeleton={<CardSkeleton className={CHART_HEIGHT_CLASS} />}
    isEmpty={(data) => data.items.length === 0}
    empty={
      <EmptyState
        icon={Percent}
        title="Chưa có dữ liệu lấp đầy"
        description="Không có suất chiếu nào trong khoảng ngày và loại suất đã chọn."
      />
    }
    errorTitle="Không tải được tỉ lệ lấp đầy"
  >
    {(data) => {
      const rows = data.items.map((item) => ({
        name: getOccupancyGroupLabel(groupBy, item),
        current: item.occupancyPercent,
        previous: item.previousOccupancyPercent,
      }));

      return (
        <div
          className={cn("space-y-4 transition-opacity", query.isPlaceholderData && "opacity-60")}
        >
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div>
              <p className="t-numeral text-4xl text-foreground">
                {formatOptionalPercent(data.total.occupancyPercent)}
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                Kỳ này ({formatDate(data.from)} – {formatDate(data.to)})
              </p>
            </div>
            <div>
              <p className="t-numeral text-4xl text-foreground">
                {formatOptionalPercent(data.total.previousOccupancyPercent)}
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                Kỳ trước ({formatDate(data.previousFrom)} – {formatDate(data.previousTo)})
              </p>
            </div>
            <div>
              <p
                className={cn(
                  "t-numeral text-4xl",
                  getChangeTextClass(data.total.changePoints),
                )}
              >
                {formatChangePoints(data.total.changePoints)}
              </p>
              <p className="mt-1 text-xs text-muted-foreground">Chênh lệch so với kỳ trước</p>
            </div>
          </div>

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
              <YAxis
                tickLine={false}
                axisLine={false}
                tickMargin={8}
                width={48}
                domain={[0, 100]}
                tickFormatter={(value: number) => `${value}%`}
              />
              <ChartTooltip
                content={
                  <ChartTooltipContent
                    formatter={(value, name) => (
                      <div className="flex w-full items-center justify-between gap-4">
                        <span className="text-muted-foreground">
                          {chartConfig[name as keyof typeof chartConfig]?.label ?? name}
                        </span>
                        <span className="font-medium tabular-nums text-foreground">
                          {formatOptionalPercent(value == null ? null : Number(value))}
                        </span>
                      </div>
                    )}
                  />
                }
              />
              <ChartLegend content={<ChartLegendContent />} />
              <Bar dataKey="current" fill="var(--color-current)" radius={0} />
              <Bar dataKey="previous" fill="var(--color-previous)" radius={0} />
            </BarChart>
          </ChartContainer>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{groupBy === "Weekday" ? "Thứ" : "Nhóm"}</TableHead>
                <TableHead className="text-right">Suất</TableHead>
                <TableHead className="text-right">Vé / ghế bán được</TableHead>
                <TableHead className="text-right">Kỳ này</TableHead>
                <TableHead className="text-right">Kỳ trước</TableHead>
                <TableHead className="text-right">Chênh lệch</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((item) => (
                <TableRow key={item.key}>
                  <TableCell className="font-medium">
                    {getOccupancyGroupLabel(groupBy, item)}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatNumber(item.showtimeCount)}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatNumber(item.ticketsSold)} / {formatNumber(item.sellableSeats)}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatOptionalPercent(item.occupancyPercent)}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatOptionalPercent(item.previousOccupancyPercent)}
                  </TableCell>
                  <TableCell
                    className={cn("text-right tabular-nums", getChangeTextClass(item.changePoints))}
                  >
                    {formatChangePoints(item.changePoints)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      );
    }}
  </QueryState>
);
