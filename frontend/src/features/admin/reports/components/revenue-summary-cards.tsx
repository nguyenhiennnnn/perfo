import type { UseQueryResult } from "@tanstack/react-query";
import { Card, CardContent } from "@/components/ui/card";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { CardSkeleton } from "@/components/common/skeleton-shapes";
import { formatCurrency, formatNumber } from "@/lib/formatters";
import type { RevenueReportDto } from "../types";
import { Wallet, Receipt } from "lucide-react";

interface RevenueSummaryCardsProps {
  query: UseQueryResult<RevenueReportDto>;
}

const SUMMARY_GRID_CLASS = "grid grid-cols-1 gap-3 sm:grid-cols-2";

export const RevenueSummaryCards = ({ query }: RevenueSummaryCardsProps) => (
  <QueryState
    query={query}
    skeleton={
      <div className={SUMMARY_GRID_CLASS}>
        <SkeletonList count={2} shape={CardSkeleton} className="h-24" />
      </div>
    }
    errorTitle="Không tải được tổng doanh thu"
  >
    {({ summary }) => {
      const items = [
        { label: "Tổng doanh thu", value: formatCurrency(summary.totalRevenue), icon: Wallet },
        { label: "Tổng đơn đặt vé", value: formatNumber(summary.totalBookings), icon: Receipt },
      ];

      return (
        <div className={SUMMARY_GRID_CLASS}>
          {items.map((item) => (
            <Card key={item.label} className="gap-2 py-4">
              <CardContent className="flex items-center justify-between gap-3 px-4">
                <div>
                  <p className="text-xs font-medium text-muted-foreground">{item.label}</p>
                  <p className="mt-1.5 text-2xl font-semibold tabular-nums text-foreground">{item.value}</p>
                </div>
                <div className="flex size-9 shrink-0 items-center justify-center bg-primary/10 text-primary">
                  <item.icon className="size-4.5" />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      );
    }}
  </QueryState>
);
