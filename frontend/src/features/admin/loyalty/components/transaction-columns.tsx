import { createColumnHelper } from "@tanstack/react-table";
import { Link } from "react-router";

import { Badge } from "@/components/ui/badge";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ROUTES } from "@/config/routes";
import { cn } from "@/lib/utils";
import { formatDateTime, formatPoints } from "@/lib/formatters";
import { SYSTEM_ACTOR_LABEL } from "@/features/admin/audit-logs/utils/audit-log-labels";

import type { LoyaltyTransactionDto } from "../types";
import { getTransactionTypeBadge } from "../utils/loyalty-admin-labels";

const columnHelper = createColumnHelper<DataTableFeatures, LoyaltyTransactionDto>();

export const transactionColumns = columnHelper.columns([
  columnHelper.accessor("createdAt", {
    header: "Thời điểm",
    cell: ({ row }) => (
      <span className="text-sm tabular-nums text-muted-foreground">
        {formatDateTime(row.original.createdAt)}
      </span>
    ),
    enableSorting: false,
  }),
  columnHelper.accessor("type", {
    header: "Loại",
    cell: ({ row }) => {
      const badge = getTransactionTypeBadge(row.original.type);
      return <Badge variant={badge.variant}>{badge.label}</Badge>;
    },
    enableSorting: false,
  }),
  columnHelper.accessor("points", {
    header: "Điểm",
    cell: ({ row }) => {
      const { points } = row.original;
      return (
        <span
          className={cn(
            "text-sm font-semibold tabular-nums",
            points > 0 ? "text-success" : "text-destructive",
          )}
        >
          {points > 0 ? "+" : ""}
          {formatPoints(points)}
        </span>
      );
    },
    enableSorting: false,
  }),
  columnHelper.display({
    id: "order",
    header: "Đơn hàng",
    cell: ({ row }) =>
      row.original.orderId ? (
        <Link
          to={ROUTES.admin.orderDetail(row.original.orderId)}
          className="text-sm text-primary-text hover:underline"
        >
          #{row.original.orderId}
        </Link>
      ) : (
        <span className="text-sm text-muted-foreground">—</span>
      ),
    enableSorting: false,
  }),
  columnHelper.display({
    id: "actor",
    header: "Người thực hiện",
    cell: ({ row }) => (
      <span className="text-sm text-muted-foreground">
        {row.original.actorUserId ? (row.original.actorName ?? `#${row.original.actorUserId}`) : SYSTEM_ACTOR_LABEL}
      </span>
    ),
    enableSorting: false,
  }),
]);
