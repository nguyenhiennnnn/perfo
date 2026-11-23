import { createColumnHelper } from "@tanstack/react-table";
import { Link } from "react-router";

import { Badge } from "@/components/ui/badge";
import { BookingChannelBadge } from "@/components/common/status-badge";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ROUTES } from "@/config/routes";
import { STATUS_LABEL } from "@/config/default-constants";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

import type { PromotionRedemptionDto } from "../types";
import { REDEMPTION_STATUS_BADGES } from "../utils/promotion-admin-labels";

const columnHelper = createColumnHelper<DataTableFeatures, PromotionRedemptionDto>();

export const createRedemptionColumns = () =>
  columnHelper.columns([
    columnHelper.accessor("orderCode", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Đơn hàng" />,
      cell: ({ row }) => (
        <Link
          to={ROUTES.admin.orderDetail(row.original.orderId)}
          className="t-code font-medium text-primary-text hover:underline"
        >
          {row.original.orderCode}
        </Link>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("channel", {
      header: "Kênh bán",
      cell: ({ row }) => <BookingChannelBadge channel={row.original.channel} />,
      enableSorting: false,
    }),
    columnHelper.accessor("customerName", {
      header: "Khách hàng",
      cell: ({ row }) => (
        <span className="text-sm">{row.original.customerName || "Khách vãng lai"}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("status", {
      header: () => STATUS_LABEL,
      cell: ({ row }) => {
        const item = REDEMPTION_STATUS_BADGES[row.original.status];
        return item ? (
          <Badge variant={item.variant}>{item.label}</Badge>
        ) : (
          <Badge variant="outline">{row.original.status}</Badge>
        );
      },
      enableSorting: false,
    }),
    columnHelper.accessor("discountAmount", {
      header: () => <div className="text-right">Tiền giảm</div>,
      cell: ({ row }) => (
        <div className="text-right font-semibold tabular-nums">
          {formatCurrency(row.original.discountAmount)}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("orderFinalAmount", {
      header: () => <div className="text-right">Đơn sau giảm</div>,
      cell: ({ row }) => (
        <div className="text-right tabular-nums">{formatCurrency(row.original.orderFinalAmount)}</div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("reservedAt", {
      header: "Giữ chỗ lúc",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDateTime(row.original.reservedAt)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("redeemedAt", {
      header: "Dùng lúc",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDateTime(row.original.redeemedAt)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("releasedAt", {
      header: "Nhả lúc",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDateTime(row.original.releasedAt)}
        </span>
      ),
      enableSorting: false,
    }),
  ]);
