import { createColumnHelper } from "@tanstack/react-table";
import { Link } from "react-router";
import { CheckCircle2, Clock } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { STATUS_LABEL } from "@/config/default-constants";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

import { CashRefundStatus, type CashRefundListItemDto } from "../types/cash-refunds";

interface CashRefundColumnsOptions {
  onConfirm: (item: CashRefundListItemDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, CashRefundListItemDto>();

export const CashRefundStatusBadge = ({ status }: { status: string }) =>
  status === CashRefundStatus.Refunded ? (
    <Badge variant="success">
      <CheckCircle2 className="size-3" />
      Đã hoàn
    </Badge>
  ) : (
    <Badge variant="warning">
      <Clock className="size-3" />
      Chưa hoàn
    </Badge>
  );

export const createCashRefundColumns = ({ onConfirm }: CashRefundColumnsOptions) =>
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
          {row.original.customerPhone && (
            <span className="text-xs text-muted-foreground">{row.original.customerPhone}</span>
          )}
        </div>
      ),
    }),
    columnHelper.accessor("amount", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Số tiền hoàn" />,
      cell: ({ row }) => (
        <span className="font-semibold tabular-nums text-primary-text">
          {formatCurrency(row.original.amount)}
        </span>
      ),
    }),
    columnHelper.accessor("movieTitle", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Suất bị hủy" />,
      cell: ({ row }) =>
        row.original.movieTitle ? (
          <div className="flex min-w-40 flex-col">
            <span className="font-medium text-foreground">{row.original.movieTitle}</span>
            <span className="text-xs tabular-nums text-muted-foreground">
              {formatDateTime(row.original.showtimeStartTime)}
            </span>
          </div>
        ) : (
          <span className="text-sm text-muted-foreground">—</span>
        ),
    }),
    columnHelper.accessor("status", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title={STATUS_LABEL} />,
      cell: ({ row }) => <CashRefundStatusBadge status={row.original.status} />,
    }),
    columnHelper.accessor("confirmedByName", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Người xác nhận" />,
      cell: ({ row }) =>
        row.original.status === CashRefundStatus.Refunded ? (
          <div className="flex min-w-32 flex-col">
            <span className="text-sm text-foreground">{row.original.confirmedByName ?? "—"}</span>
            <span className="text-xs tabular-nums text-muted-foreground">
              {formatDateTime(row.original.refundedAt)}
            </span>
          </div>
        ) : (
          <span className="text-sm text-muted-foreground">—</span>
        ),
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
          {row.original.status === CashRefundStatus.Pending && (
            <Button size="sm" onClick={() => onConfirm(row.original)}>
              Xác nhận đã hoàn
            </Button>
          )}
        </div>
      ),
      enableHiding: false,
    }),
  ]);
