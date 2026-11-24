"use client";

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Popcorn } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
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
import {
  formatCurrency,
  formatDate,
  formatNumber,
  formatPercent,
} from "@/lib/formatters";
import { useDashboardComboRevenue } from "../hooks/use-dashboard";
import { useDashboardRange } from "../utils/dashboard-range";
import { formatOptionalPercent, getChannelLabel } from "../utils/analytics-labels";

const chartConfig = {
  sold: { label: "Đã bán", color: "var(--chart-1)" },
  fulfilled: { label: "Đã giao", color: "var(--chart-2)" },
} satisfies ChartConfig;

const CHART_HEIGHT_CLASS = "h-72";
const AXIS_LABEL_MAX_LENGTH = 14;

const truncate = (text: string) =>
  text.length > AXIS_LABEL_MAX_LENGTH ? `${text.slice(0, AXIS_LABEL_MAX_LENGTH - 1)}…` : text;

const Metric = ({ value, label }: { value: string; label: string }) => (
  <div>
    <p className="t-numeral text-4xl text-foreground">{value}</p>
    <p className="mt-1 text-xs text-muted-foreground">{label}</p>
  </div>
);

export const ComboRevenueSection = () => {
  const { from, to } = useDashboardRange();
  const query = useDashboardComboRevenue({ from, to });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Doanh thu combo</CardTitle>
        <CardDescription>
          Doanh thu ròng, số lượng đã bán so với đã giao ({formatDate(from)} – {formatDate(to)}).
        </CardDescription>
      </CardHeader>
      <CardContent>
        <QueryState
          query={query}
          skeleton={<CardSkeleton className={CHART_HEIGHT_CLASS} />}
          isEmpty={(data) => data.total.quantitySold === 0 && data.byCombo.length === 0}
          empty={
            <EmptyState
              icon={Popcorn}
              title="Chưa có dữ liệu combo"
              description="Không có combo nào được bán trong khoảng ngày đã chọn."
            />
          }
          errorTitle="Không tải được doanh thu combo"
        >
          {(data) => {
            const rows = data.byCombo.map((item) => ({
              name: item.comboName ?? `Combo #${item.comboId}`,
              sold: item.quantitySold,
              fulfilled: item.quantityFulfilled,
            }));

            return (
              <div
                className={cn(
                  "space-y-4 transition-opacity",
                  query.isPlaceholderData && "opacity-60",
                )}
              >
                <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
                  <Metric
                    value={formatCurrency(data.total.netRevenue)}
                    label="Doanh thu combo ròng"
                  />
                  <Metric
                    value={formatOptionalPercent(data.total.comboOrderRatePercent, formatPercent)}
                    label={`Tỷ lệ đơn có combo (${formatNumber(data.total.ordersWithCombo)} / ${formatNumber(data.total.confirmedOrders)} đơn)`}
                  />
                  <Metric
                    value={
                      data.total.averageComboValuePerOrder == null
                        ? "—"
                        : formatCurrency(data.total.averageComboValuePerOrder)
                    }
                    label="Giá trị combo trung bình mỗi đơn"
                  />
                  <Metric
                    value={`${formatNumber(data.total.quantityFulfilled)} / ${formatNumber(data.total.quantitySold)}`}
                    label={`Đã giao / đã bán (${formatOptionalPercent(data.total.fulfilledPercent, formatPercent)})`}
                  />
                </div>

                <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
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
                      <YAxis tickLine={false} axisLine={false} tickMargin={8} width={40} />
                      <ChartTooltip content={<ChartTooltipContent />} />
                      <ChartLegend content={<ChartLegendContent />} />
                      <Bar dataKey="sold" fill="var(--color-sold)" radius={0} />
                      <Bar dataKey="fulfilled" fill="var(--color-fulfilled)" radius={0} />
                    </BarChart>
                  </ChartContainer>

                  <div className="space-y-2">
                    <p className="text-sm font-medium text-foreground">Theo kênh bán</p>
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>Kênh</TableHead>
                          <TableHead className="text-right">Đã bán</TableHead>
                          <TableHead className="text-right">Đã giao</TableHead>
                          <TableHead className="text-right">Tỷ lệ giao</TableHead>
                          <TableHead className="text-right">Doanh thu ròng</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {data.byChannel.map((item) => (
                          <TableRow key={item.channel}>
                            <TableCell className="font-medium">
                              {getChannelLabel(item.channel)}
                            </TableCell>
                            <TableCell className="text-right tabular-nums">
                              {formatNumber(item.quantitySold)}
                            </TableCell>
                            <TableCell className="text-right tabular-nums">
                              {formatNumber(item.quantityFulfilled)}
                            </TableCell>
                            <TableCell className="text-right tabular-nums">
                              {formatOptionalPercent(item.fulfilledPercent, formatPercent)}
                            </TableCell>
                            <TableCell className="text-right tabular-nums">
                              {formatCurrency(item.netRevenue)}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                </div>

                <div className="space-y-2">
                  <p className="text-sm font-medium text-foreground">Theo combo</p>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Combo</TableHead>
                        <TableHead className="text-right">Đã bán</TableHead>
                        <TableHead className="text-right">Đã giao</TableHead>
                        <TableHead className="text-right">Tỷ lệ giao</TableHead>
                        <TableHead className="text-right">Doanh thu ròng</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {data.byCombo.map((item) => (
                        <TableRow key={item.comboId}>
                          <TableCell className="font-medium">
                            {item.comboName ?? `Combo #${item.comboId}`}
                          </TableCell>
                          <TableCell className="text-right tabular-nums">
                            {formatNumber(item.quantitySold)}
                          </TableCell>
                          <TableCell className="text-right tabular-nums">
                            {formatNumber(item.quantityFulfilled)}
                          </TableCell>
                          <TableCell className="text-right tabular-nums">
                            {formatOptionalPercent(item.fulfilledPercent, formatPercent)}
                          </TableCell>
                          <TableCell className="text-right tabular-nums">
                            {formatCurrency(item.netRevenue)}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </div>
            );
          }}
        </QueryState>
      </CardContent>
    </Card>
  );
};
