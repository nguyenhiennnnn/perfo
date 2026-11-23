import type { UseQueryResult } from "@tanstack/react-query";
import { BadgePercent, Receipt, Ticket, TicketX, Wallet } from "lucide-react";

import { Card, CardContent } from "@/components/ui/card";
import { QueryState } from "@/components/common/query-state";
import { SkeletonList } from "@/components/common/skeleton-list";
import { formatCurrency, formatNumber } from "@/lib/formatters";

import type { PromotionReportListDto } from "../types";

interface PromotionReportSummaryCardsProps {
  query: UseQueryResult<PromotionReportListDto>;
}

const SUMMARY_GRID_CLASS = "grid grid-cols-2 gap-3 lg:grid-cols-5";
const SUMMARY_SKELETON_COUNT = 5;

export const PromotionReportSummaryCards = ({ query }: PromotionReportSummaryCardsProps) => (
  <QueryState
    query={query}
    skeleton={
      <div aria-busy="true" className={SUMMARY_GRID_CLASS}>
        <SkeletonList count={SUMMARY_SKELETON_COUNT} className="h-24" />
      </div>
    }
    errorTitle="Không tải được tổng hợp khuyến mãi"
  >
    {({ summary }) => {
      const items = [
        { label: "Số khuyến mãi", value: formatNumber(summary.promotionCount), icon: BadgePercent },
        { label: "Lượt dùng", value: formatNumber(summary.redeemedCount), icon: Ticket },
        { label: "Số đơn hàng", value: formatNumber(summary.orderCount), icon: Receipt },
        { label: "Tổng tiền giảm", value: formatCurrency(summary.totalDiscount), icon: TicketX },
        { label: "Doanh thu đơn dùng mã", value: formatCurrency(summary.orderRevenue), icon: Wallet },
      ];

      return (
        <div className={SUMMARY_GRID_CLASS}>
          {items.map((item) => (
            <Card key={item.label} className="gap-2 py-4">
              <CardContent className="flex items-center justify-between gap-3 px-4">
                <div className="min-w-0">
                  <p className="text-xs font-medium text-muted-foreground">{item.label}</p>
                  <p className="mt-1.5 truncate text-xl font-semibold tabular-nums text-foreground">
                    {item.value}
                  </p>
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
