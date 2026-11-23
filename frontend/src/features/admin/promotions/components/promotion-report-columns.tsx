import { createColumnHelper } from "@tanstack/react-table";
import { BarChart3 } from "lucide-react";
import { Link } from "react-router";

import { Button } from "@/components/ui/button";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatNumber } from "@/lib/formatters";

import type { PromotionReportListItemDto } from "../types";
import { getPromotionTypeLabel } from "../utils/promotion-admin-labels";
import { UsageProgress } from "./usage-progress";

const columnHelper = createColumnHelper<DataTableFeatures, PromotionReportListItemDto>();

export const createPromotionReportColumns = () =>
  columnHelper.columns([
    columnHelper.accessor("code", {
      header: "Khuyến mãi",
      cell: ({ row }) => (
        <Link
          to={ROUTES.admin.promotionReport(row.original.promotionId)}
          className="flex max-w-64 flex-col gap-0.5 hover:underline"
        >
          <span className="t-code font-medium text-primary-text">{row.original.code}</span>
          <span className="truncate text-xs text-muted-foreground">{row.original.name}</span>
        </Link>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("type", {
      header: "Loại",
      cell: ({ row }) => (
        <span className="text-sm">{getPromotionTypeLabel(row.original.type)}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("redeemedCount", {
      header: () => <div className="text-right">Lượt dùng</div>,
      cell: ({ row }) => (
        <div className="text-right tabular-nums">{formatNumber(row.original.redeemedCount)}</div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("totalDiscount", {
      header: () => <div className="text-right">Tiền giảm</div>,
      cell: ({ row }) => (
        <div className="text-right font-semibold tabular-nums">
          {formatCurrency(row.original.totalDiscount)}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("orderRevenue", {
      header: () => <div className="text-right">Doanh thu</div>,
      cell: ({ row }) => (
        <div className="text-right tabular-nums">{formatCurrency(row.original.orderRevenue)}</div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "usage",
      header: "Lượt dùng so với giới hạn",
      cell: ({ row }) => (
        <UsageProgress
          used={row.original.consumedCount}
          limit={row.original.totalUsageLimit}
          ratio={row.original.usageRatio}
        />
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "budget",
      header: "Ngân sách",
      cell: ({ row }) => (
        <UsageProgress
          used={row.original.consumedBudget}
          limit={row.original.budgetAmount}
          ratio={row.original.budgetRatio}
          format={formatCurrency}
        />
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Chi tiết</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          <Button
            variant="ghost"
            size="icon"
            className="size-8"
            render={<Link to={ROUTES.admin.promotionReport(row.original.promotionId)} />}
          >
            <span className="sr-only">Xem báo cáo chi tiết</span>
            <BarChart3 className="size-4" />
          </Button>
        </div>
      ),
      enableHiding: false,
    }),
  ]);
