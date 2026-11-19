"use client";

import { Bar, CartesianGrid, ComposedChart, Line, XAxis, YAxis } from "recharts";
import { TrendingUp } from "lucide-react";
import type { ReactNode } from "react";
import { Card, CardAction, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  ChartLegend,
  ChartLegendContent,
  type ChartConfig,
} from "@/components/ui/chart";
import type { UseQueryResult } from "@tanstack/react-query";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { cn } from "@/lib/utils";
import { QueryState } from "@/components/common/query-state";
import { EmptyState } from "@/components/common/empty-state";
import { formatCurrency } from "@/lib/formatters";
import type { RevenueReportDto } from "../types";

const chartConfig = {
  ticketRevenue: { label: "Doanh thu vé", color: "var(--chart-1)" },
  comboRevenue: { label: "Doanh thu combo", color: "var(--chart-2)" },
  bookingCount: { label: "Số đơn", color: "var(--chart-3)" },
} satisfies ChartConfig;

interface RevenueReportChartProps {
  query: UseQueryResult<RevenueReportDto>;
  action?: ReactNode;
}

const CHART_HEIGHT_CLASS = "h-80";

export const RevenueReportChart = ({ query, action }: RevenueReportChartProps) => {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Biểu đồ doanh thu</CardTitle>
        <CardDescription>Doanh thu vé, combo và số đơn theo kỳ báo cáo</CardDescription>
        {action && <CardAction>{action}</CardAction>}
      </CardHeader>
      <CardContent>
        <QueryState
          query={query}
          skeleton={<CardSkeleton className={CHART_HEIGHT_CLASS} />}
          isEmpty={(report) => report.items.length === 0}
          empty={
            <EmptyState
              icon={TrendingUp}
              title="Chưa có doanh thu"
              description="Chưa có doanh thu trong khoảng thời gian đã chọn. Thử mở rộng khoảng ngày."
            />
          }
          errorTitle="Không tải được biểu đồ doanh thu"
        >
          {(report) => (
          <ChartContainer config={chartConfig} className={cn(CHART_HEIGHT_CLASS, "w-full")}>
            <ComposedChart accessibilityLayer data={report.items}>
              <CartesianGrid vertical={false} />
              <XAxis dataKey="period" tickLine={false} axisLine={false} tickMargin={10} />
              <YAxis
                yAxisId="revenue"
                tickLine={false}
                axisLine={false}
                width={80}
                tickFormatter={(value: number) => formatCurrency(value)}
              />
              <YAxis yAxisId="count" orientation="right" tickLine={false} axisLine={false} width={40} />
              <ChartTooltip content={<ChartTooltipContent />} />
              <ChartLegend content={<ChartLegendContent />} />
              <Bar yAxisId="revenue" dataKey="ticketRevenue" stackId="revenue" fill="var(--color-ticketRevenue)" radius={0} />
              <Bar yAxisId="revenue" dataKey="comboRevenue" stackId="revenue" fill="var(--color-comboRevenue)" radius={0} />
              <Line yAxisId="count" dataKey="bookingCount" stroke="var(--color-bookingCount)" strokeWidth={2} dot={false} />
            </ComposedChart>
          </ChartContainer>
          )}
        </QueryState>
      </CardContent>
    </Card>
  );
}