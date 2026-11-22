import { createColumnHelper } from "@tanstack/react-table";
import { Eye } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { BookingChannelBadge, BookingStatusBadge } from "@/components/common/status-badge";
import { STATUS_LABEL } from "@/config/default-constants";
import { getOrderTypeLabel } from "@/features/client/orders/utils/order-labels";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

import type { AdminOrderListItemDto } from "../types";
import { getOrderRefundStatusInfo } from "../utils/order-cancel";

interface OrderColumnsOptions {
  onViewDetail: (order: AdminOrderListItemDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, AdminOrderListItemDto>();

export const createOrderColumns = ({ onViewDetail }: OrderColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("orderCode", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mã đơn" />,
      cell: ({ row }) => <span className="t-code text-foreground">{row.original.orderCode}</span>,
    }),
    columnHelper.accessor("type", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Loại đơn" />,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">{getOrderTypeLabel(row.original.type)}</span>
      ),
    }),
    columnHelper.accessor("channel", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Kênh" />,
      cell: ({ row }) => <BookingChannelBadge channel={row.original.channel} />,
    }),
    columnHelper.accessor("customerName", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Khách hàng" />,
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="font-medium text-foreground">
            {row.original.customerName || "Khách vãng lai"}
          </span>
          {row.original.customerPhone && (
            <span className="text-xs text-muted-foreground">{row.original.customerPhone}</span>
          )}
        </div>
      ),
    }),
    columnHelper.accessor("movieTitle", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Phim và suất chiếu" />,
      cell: ({ row }) =>
        row.original.movieTitle ? (
          <div className="flex min-w-40 flex-col">
            <span className="font-medium text-foreground">{row.original.movieTitle}</span>
            <span className="text-xs tabular-nums text-muted-foreground">
              {formatDateTime(row.original.startTime)}
            </span>
          </div>
        ) : (
          <span className="text-muted-foreground">—</span>
        ),
    }),
    columnHelper.accessor("ticketCount", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Số vé" />,
      cell: ({ row }) => (
        <span className="tabular-nums text-foreground">{row.original.ticketCount}</span>
      ),
    }),
    columnHelper.accessor("finalAmount", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Tổng tiền" />,
      cell: ({ row }) => (
        <span className="font-semibold tabular-nums text-primary-text">
          {formatCurrency(row.original.finalAmount)}
        </span>
      ),
    }),
    columnHelper.accessor("status", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title={STATUS_LABEL} />,
      cell: ({ row }) => <BookingStatusBadge status={row.original.status} />,
    }),
    columnHelper.accessor("refundStatus", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Hoàn tiền" />,
      cell: ({ row }) => {
        const info = getOrderRefundStatusInfo(row.original.refundStatus);
        return info ? (
          <Badge variant={info.variant}>{info.label}</Badge>
        ) : (
          <span className="text-muted-foreground">—</span>
        );
      },
    }),
    columnHelper.accessor("createdAt", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Thời điểm tạo" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDateTime(row.original.createdAt)}
        </span>
      ),
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Thao tác</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          <Button
            variant="ghost"
            size="icon"
            className="size-8"
            onClick={() => onViewDetail(row.original)}
          >
            <span className="sr-only">Xem chi tiết đơn {row.original.orderCode}</span>
            <Eye className="size-4" />
          </Button>
        </div>
      ),
      enableHiding: false,
    }),
  ]);
