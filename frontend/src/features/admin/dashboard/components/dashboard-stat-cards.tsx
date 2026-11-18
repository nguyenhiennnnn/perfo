import { Card, CardContent } from "@/components/ui/card";
import type { UseQueryResult } from "@tanstack/react-query";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { cn } from "@/lib/utils";
import { formatCurrency, formatNumber, formatPercent } from "@/lib/formatters";
import type { DashboardSummaryDto } from "@/features/admin/dashboard/types";
import { Wallet, Ticket, CalendarCheck, Clock3, Percent, type LucideIcon } from "lucide-react";

interface StatItem {
  label: string;
  value: string;
  icon: LucideIcon;
  hint?: string;
  trend?: "up" | "down" | "neutral";
}

const StatCard = ({ label, value, hint, trend }: StatItem) => {
  return (
    <Card className="py-4">
      <CardContent className="px-4">
        <div className="min-w-0">
          <p className="t-numeral text-2xl font-bold tracking-tight text-foreground md:text-3xl">{value}</p>
          <p className="mt-1 text-xs text-muted-foreground">{label}</p>
          {hint && (
            <p
              className={cn(
                "mt-2 text-xs font-medium",
                trend === "up" && "text-success",
                trend === "down" && "text-destructive",
                trend === "neutral" && "text-muted-foreground",
              )}
            >
              {hint}
            </p>
          )}
        </div>
      </CardContent>
    </Card>
  );
};

interface DashboardStatCardsProps {
  query: UseQueryResult<DashboardSummaryDto>;
}

const STATS_GRID_CLASS = "grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-5";

const buildItems = (summary: DashboardSummaryDto): StatItem[] => {
  const changePercent = summary.revenueChangePercent;
  const changeTrend =
    changePercent == null ? undefined : changePercent > 0 ? "up" : changePercent < 0 ? "down" : "neutral";

  return [
    {
      label: "Doanh thu hôm nay",
      value: formatCurrency(summary.todayRevenue),
      icon: Wallet,
      hint: changePercent == null ? undefined : `${formatPercent(changePercent, { showSign: true })} so với hôm qua`,
      trend: changeTrend,
    },
    { label: "Vé bán hôm nay", value: formatNumber(summary.ticketsSoldToday), icon: Ticket },
    { label: "Đơn đã xác nhận", value: formatNumber(summary.confirmedBookingsToday), icon: CalendarCheck },
    {
      label: "Đơn đang chờ",
      value: formatNumber(summary.activePendingBookings),
      icon: Clock3,
      hint: summary.activePendingBookings > 0 ? "Cần theo dõi" : undefined,
      trend: summary.activePendingBookings > 0 ? "neutral" : undefined,
    },
    {
      label: "Tỉ lệ lấp đầy hôm nay",
      value: summary.todayOccupancyPercent == null ? "—" : formatPercent(summary.todayOccupancyPercent),
      icon: Percent,
    },
  ];
};

export const DashboardStatCards = ({ query }: DashboardStatCardsProps) => (
  <QueryState
    query={query}
    skeleton={
      <div className={STATS_GRID_CLASS}>
        <SkeletonList count={5} shape={CardSkeleton} className="h-24" />
      </div>
    }
    errorTitle="Không tải được số liệu tổng quan"
  >
    {(summary) => (
      <div className={STATS_GRID_CLASS}>
        {buildItems(summary).map((item) => (
          <StatCard key={item.label} {...item} />
        ))}
      </div>
    )}
  </QueryState>
);
