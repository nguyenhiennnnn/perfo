import { createColumnHelper } from "@tanstack/react-table";
import { Eye, MoreHorizontal } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { PaymentMethodBadge } from "@/components/common/status-badge";
import { STATUS_LABEL } from "@/config/default-constants";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

import type { AdminPaymentListItemDto } from "../types/ledger";
import { isRefundTransaction, needsCashRefund } from "../utils/transaction-labels";
import {
  CashRefundFlagBadge,
  TransactionStatusBadge,
  TransactionTypeBadge,
} from "./transaction-badges";

interface PaymentColumnsOptions {
  onViewDetail: (payment: AdminPaymentListItemDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, AdminPaymentListItemDto>();

export const createPaymentColumns = ({ onViewDetail }: PaymentColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("transactionCode", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mã giao dịch" />,
      cell: ({ row }) => <span className="t-code text-foreground">{row.original.transactionCode}</span>,
    }),
    columnHelper.accessor("orderCode", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mã đơn" />,
      cell: ({ row }) => <span className="t-code text-foreground">{row.original.orderCode}</span>,
    }),
    columnHelper.accessor("type", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Loại" />,
      cell: ({ row }) => <TransactionTypeBadge type={row.original.type} />,
    }),
    columnHelper.accessor("method", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Phương thức" />,
      cell: ({ row }) => <PaymentMethodBadge method={row.original.method} />,
    }),
    columnHelper.accessor("amount", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Số tiền" />,
      cell: ({ row }) => (
        <span className="font-semibold tabular-nums text-primary-text">
          {isRefundTransaction(row.original.type) ? "-" : ""}
          {formatCurrency(row.original.amount)}
        </span>
      ),
    }),
    columnHelper.accessor("gatewayFee", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Phí cổng" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {row.original.gatewayFee > 0 ? formatCurrency(row.original.gatewayFee) : "—"}
        </span>
      ),
    }),
    columnHelper.accessor("status", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title={STATUS_LABEL} />,
      cell: ({ row }) => (
        <div className="flex flex-wrap items-center gap-1.5">
          <TransactionStatusBadge type={row.original.type} status={row.original.status} />
          {needsCashRefund(row.original) && <CashRefundFlagBadge />}
        </div>
      ),
    }),
    columnHelper.accessor("createdAt", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Thời điểm" />,
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
          <DropdownMenu>
            <DropdownMenuTrigger>
              <Button variant="ghost" size="icon" className="size-8">
                <span className="sr-only">Mở menu</span>
                <MoreHorizontal className="size-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => onViewDetail(row.original)}>
                <Eye /> Xem chi tiết
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ),
      enableHiding: false,
    }),
  ]);
