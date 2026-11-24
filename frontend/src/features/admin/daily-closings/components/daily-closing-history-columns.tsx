import { createColumnHelper } from "@tanstack/react-table";
import { FileText } from "lucide-react";

import { ButtonLink } from "@/components/common/button-link";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDate, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";

import type { DailyClosingListItem } from "../types";

const columnHelper = createColumnHelper<DataTableFeatures, DailyClosingListItem>();

export const createDailyClosingHistoryColumns = () =>
  columnHelper.columns([
    columnHelper.accessor("businessDate", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Ngày kinh doanh" />,
      cell: ({ row }) => (
        <span className="text-sm font-medium tabular-nums">
          {formatDate(row.original.businessDate)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("expectedCash", {
      header: () => <div className="text-right">Tiền mặt theo sổ</div>,
      cell: ({ row }) => (
        <div className="text-right text-sm tabular-nums">
          {formatCurrency(row.original.expectedCash)}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("countedCash", {
      header: () => <div className="text-right">Thực đếm</div>,
      cell: ({ row }) => (
        <div className="text-right text-sm tabular-nums">
          {formatCurrency(row.original.countedCash)}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("variance", {
      header: () => <div className="text-right">Chênh lệch</div>,
      cell: ({ row }) => (
        <div
          className={cn(
            "text-right text-sm font-medium tabular-nums",
            row.original.variance === 0 ? "text-success" : "text-warning-text",
          )}
        >
          {formatCurrency(row.original.variance)}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("closedByName", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Người chốt" />,
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="text-sm">{row.original.closedByName}</span>
          <span className="text-xs tabular-nums text-muted-foreground">
            {formatDateTime(row.original.closedAt)}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("note", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Ghi chú" />,
      cell: ({ row }) => (
        <span className="line-clamp-2 max-w-56 text-sm text-muted-foreground">
          {row.original.note ?? "—"}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          <ButtonLink
            variant="ghost"
            size="sm"
            to={ROUTES.admin.dailyClosingReport(row.original.businessDate)}
          >
            <FileText className="size-4" />
            <span className="hidden sm:inline">Xem báo cáo</span>
          </ButtonLink>
        </div>
      ),
    }),
  ]);
