import { createColumnHelper } from "@tanstack/react-table";
import { Link } from "react-router";
import { ArrowRight, Ban, CheckCircle2, Clock } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { STATUS_LABEL } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

import { RoomChangeRefundStatus, type RoomChangeRefundListItemDto } from "../types";

interface RoomChangeRefundColumnsOptions {
  onConfirm: (item: RoomChangeRefundListItemDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, RoomChangeRefundListItemDto>();

export const RoomChangeRefundStatusBadge = ({ status }: { status: string }) => {
  if (status === RoomChangeRefundStatus.Refunded)
    return (
      <Badge variant="success">
        <CheckCircle2 className="size-3" />
        Đã hoàn
      </Badge>
    );
  if (status === RoomChangeRefundStatus.Voided)
    return (
      <Badge variant="secondary">
        <Ban className="size-3" />
        Đã hủy
      </Badge>
    );
  return (
    <Badge variant="warning">
      <Clock className="size-3" />
      Chờ hoàn
    </Badge>
  );
};

export const createRoomChangeRefundColumns = ({ onConfirm }: RoomChangeRefundColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("orderCode", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mã đơn" />,
      cell: ({ row }) => (
        <Link
          to={ROUTES.admin.orderDetail(row.original.orderId)}
          className="t-code text-foreground underline-offset-4 hover:underline"
        >
          {row.original.orderCode}
        </Link>
      ),
    }),
    columnHelper.accessor("customerName", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Khách hàng" />,
      cell: ({ row }) => (
        <div className="flex min-w-32 flex-col">
          <span className="font-medium text-foreground">
            {row.original.isGuest || !row.original.customerName
              ? "Khách vãng lai"
              : row.original.customerName}
          </span>
          {row.original.isGuest && row.original.customerName && (
            <span className="text-xs text-muted-foreground">{row.original.customerName}</span>
          )}
          {row.original.customerPhone && (
            <span className="text-xs text-muted-foreground">{row.original.customerPhone}</span>
          )}
        </div>
      ),
    }),
    columnHelper.accessor("movieTitle", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Suất chiếu" />,
      cell: ({ row }) => (
        <div className="flex min-w-40 flex-col">
          <span className="font-medium text-foreground">{row.original.movieTitle}</span>
          <span className="text-xs tabular-nums text-muted-foreground">
            {formatDateTime(row.original.showtimeStartTime)}
          </span>
        </div>
      ),
    }),
    columnHelper.accessor("fromSeat", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Ghế cũ → ghế mới" />,
      cell: ({ row }) => (
        <span className="inline-flex items-center gap-1.5 whitespace-nowrap font-medium tabular-nums text-foreground">
          {row.original.fromSeat}
          <ArrowRight className="size-3.5 text-muted-foreground" />
          {row.original.toSeat}
        </span>
      ),
    }),
    columnHelper.accessor("amountDue", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Số tiền cần hoàn" />,
      cell: ({ row }) => (
        <span className="font-semibold tabular-nums text-primary-text">
          {formatCurrency(row.original.amountDue)}
        </span>
      ),
    }),
    columnHelper.accessor("status", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title={STATUS_LABEL} />,
      cell: ({ row }) => <RoomChangeRefundStatusBadge status={row.original.status} />,
    }),
    columnHelper.accessor("createdAt", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Phát sinh lúc" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDateTime(row.original.createdAt)}
        </span>
      ),
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          {row.original.status === RoomChangeRefundStatus.Pending && (
            <Button size="sm" onClick={() => onConfirm(row.original)}>
              Xác nhận hoàn
            </Button>
          )}
        </div>
      ),
      enableHiding: false,
    }),
  ]);
