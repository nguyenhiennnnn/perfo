"use client";

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Trophy } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent, type ChartConfig } from "@/components/ui/chart";
import type { UseQueryResult } from "@tanstack/react-query";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { cn } from "@/lib/utils";
import { EmptyState } from "@/components/common/empty-state";
import { QueryState } from "@/components/common/query-state";
import type { DashboardInsightsDto } from "@/features/admin/dashboard/types";
import { TOP_MOVIES_CHART_LABEL_MAX_CHARS } from "@/config/default-constants";

const chartConfig = {
  ticketsSold: { label: "Vé đã bán", color: "var(--chart-3)" },
} satisfies ChartConfig;

const CHART_HEIGHT_CLASS = "h-65";

interface TopMoviesCardProps {
  query: UseQueryResult<DashboardInsightsDto>;
}

export const TopMoviesCard = ({ query }: TopMoviesCardProps) => {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Phim bán chạy</CardTitle>
        <CardDescription>Xếp hạng theo số vé bán ra</CardDescription>
      </CardHeader>
      <CardContent>
        <QueryState
          query={query}
          skeleton={<CardSkeleton className={CHART_HEIGHT_CLASS} />}
          isEmpty={(insights) => insights.topMovies.length === 0}
          empty={<EmptyState icon={Trophy} title="Chưa có dữ liệu" description="Chưa có phim nào bán vé trong khoảng này." />}
          errorTitle="Không tải được phim bán chạy"
        >
          {(insights) => {
            const data = insights.topMovies.map((m) => ({
              ...m,
              label: m.title.length > TOP_MOVIES_CHART_LABEL_MAX_CHARS ? `${m.title.slice(0, TOP_MOVIES_CHART_LABEL_MAX_CHARS)}…` : m.title,
            }));

            return (
          <ChartContainer config={chartConfig} className={cn(CHART_HEIGHT_CLASS, "w-full")}>
            <BarChart accessibilityLayer data={data} layout="vertical" margin={{ left: 8 }}>
              <CartesianGrid horizontal={false} />
              <XAxis type="number" hide />
              <YAxis dataKey="label" type="category" tickLine={false} axisLine={false} width={110} />
              <ChartTooltip content={<ChartTooltipContent hideLabel />} />
              <Bar dataKey="ticketsSold" fill="var(--color-ticketsSold)" radius={0} />
            </BarChart>
          </ChartContainer>
            );
          }}
        </QueryState>
      </CardContent>
    </Card>
  );
}