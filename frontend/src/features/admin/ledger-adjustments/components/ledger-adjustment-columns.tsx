import { createColumnHelper } from "@tanstack/react-table";

import { Badge } from "@/components/ui/badge";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatDate, formatDateTime } from "@/lib/formatters";

import type { LedgerAdjustment } from "../types";
import { getLedgerAdjustmentTypeLabel } from "../utils/labels";
import { SignedAmount } from "./signed-amount";

const columnHelper = createColumnHelper<DataTableFeatures, LedgerAdjustment>();

const amountColumn = (
  key:
    | "ticketRevenueDelta"
    | "comboRevenueDelta"
    | "giftCardSoldDelta"
    | "discountDelta"
    | "refundAmountDelta",
  title: string,
) =>
  columnHelper.accessor(key, {
    header: () => <div className="text-right">{title}</div>,
    cell: ({ row }) => (
      <div className="text-right text-sm">
        <SignedAmount value={row.original[key]} />
      </div>
    ),
    enableSorting: false,
  });

export const createLedgerAdjustmentColumns = () =>
  columnHelper.columns([
    columnHelper.accessor("postingDate", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Ngày ghi sổ" />,
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="text-sm tabular-nums">{formatDate(row.original.postingDate)}</span>
          <span className="text-xs tabular-nums text-muted-foreground">
            {formatDateTime(row.original.createdAt)}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("originalDate", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Ngày gốc" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">{formatDate(row.original.originalDate)}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("type", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Loại" />,
      cell: ({ row }) => (
        <Badge variant={row.original.type === "Manual" ? "outline" : "secondary"}>
          {getLedgerAdjustmentTypeLabel(row.original.type)}
        </Badge>
      ),
      enableSorting: false,
    }),
    amountColumn("ticketRevenueDelta", "Doanh thu vé"),
    amountColumn("comboRevenueDelta", "Doanh thu combo"),
    amountColumn("giftCardSoldDelta", "Thẻ quà bán"),
    amountColumn("discountDelta", "Giảm giá"),
    amountColumn("refundAmountDelta", "Hoàn tiền"),
    columnHelper.accessor("cancelledOrdersDelta", {
      header: () => <div className="text-right">Đơn hủy</div>,
      cell: ({ row }) => (
        <div className="text-right text-sm">
          <SignedAmount value={row.original.cancelledOrdersDelta} count />
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("reason", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Lý do" />,
      cell: ({ row }) => (
        <div className="flex max-w-64 flex-col">
          <span className="line-clamp-2 text-sm text-foreground">{row.original.reason}</span>
          <span className="text-xs text-muted-foreground">
            {row.original.createdByName ?? "Hệ thống"}
            {row.original.orderId ? ` · Đơn #${row.original.orderId}` : ""}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
  ]);
