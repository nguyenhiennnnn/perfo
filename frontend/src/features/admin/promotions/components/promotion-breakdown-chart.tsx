import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import { formatCurrency, formatNumber } from "@/lib/formatters";

export interface BreakdownRow {
  key: string;
  label: string;
  axisLabel: string;
  redeemedCount: number;
  totalDiscount: number;
  orderRevenue: number;
}

interface PromotionBreakdownChartProps {
  title: string;
  description: string;
  rows: BreakdownRow[];
  color: string;
}

const CHART_HEIGHT_CLASS = "h-56";

export const PromotionBreakdownChart = ({
  title,
  description,
  rows,
  color,
}: PromotionBreakdownChartProps) => {
  const chartConfig = {
    redeemedCount: { label: "Lượt dùng", color },
  } satisfies ChartConfig;

  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <ChartContainer config={chartConfig} className={`${CHART_HEIGHT_CLASS} w-full`}>
          <BarChart accessibilityLayer data={rows} margin={{ left: 0, right: 8 }}>
            <CartesianGrid vertical={false} />
            <XAxis dataKey="axisLabel" tickLine={false} axisLine={false} />
            <YAxis allowDecimals={false} tickLine={false} axisLine={false} width={32} />
            <ChartTooltip
              content={
                <ChartTooltipContent
                  hideLabel
                  formatter={(_value, _name, item) => {
                    const row = item.payload as BreakdownRow;
                    return (
                      <div className="grid w-full gap-1">
                        <span className="font-medium text-foreground">{row.label}</span>
                        <span className="text-muted-foreground">
                          Lượt dùng:{" "}
                          <span className="font-medium tabular-nums text-foreground">
                            {formatNumber(row.redeemedCount)}
                          </span>
                        </span>
                        <span className="text-muted-foreground">
                          Tiền giảm:{" "}
                          <span className="font-medium tabular-nums text-foreground">
                            {formatCurrency(row.totalDiscount)}
                          </span>
                        </span>
                        <span className="text-muted-foreground">
                          Doanh thu:{" "}
                          <span className="font-medium tabular-nums text-foreground">
                            {formatCurrency(row.orderRevenue)}
                          </span>
                        </span>
                      </div>
                    );
                  }}
                />
              }
            />
            <Bar dataKey="redeemedCount" fill="var(--color-redeemedCount)" radius={0} />
          </BarChart>
        </ChartContainer>

        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{title}</TableHead>
              <TableHead className="text-right">Lượt dùng</TableHead>
              <TableHead className="text-right">Tiền giảm</TableHead>
              <TableHead className="text-right">Doanh thu</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={row.key}>
                <TableCell className="text-sm">{row.label}</TableCell>
                <TableCell className="text-right tabular-nums">
                  {formatNumber(row.redeemedCount)}
                </TableCell>
                <TableCell className="text-right tabular-nums">
                  {formatCurrency(row.totalDiscount)}
                </TableCell>
                <TableCell className="text-right tabular-nums">
                  {formatCurrency(row.orderRevenue)}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
};
