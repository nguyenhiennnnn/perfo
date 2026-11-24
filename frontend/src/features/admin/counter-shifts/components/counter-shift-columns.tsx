import { createColumnHelper } from "@tanstack/react-table";
import { DoorClosed } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";

import { COUNTER_SHIFT_STATUS_OPEN, type CounterShift } from "../types";

interface CounterShiftColumnsOptions {
  onCloseByAdmin: (shift: CounterShift) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, CounterShift>();

const money = (value: number | null) => (value == null ? "—" : formatCurrency(value));

export const ShiftStatusBadge = ({ status }: { status: string }) =>
  status === COUNTER_SHIFT_STATUS_OPEN ? (
    <Badge variant="warning">Đang mở</Badge>
  ) : (
    <Badge variant="success">Đã đóng</Badge>
  );

export const createCounterShiftColumns = ({ onCloseByAdmin }: CounterShiftColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("employeeName", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Nhân viên" />,
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="text-sm font-medium text-foreground">{row.original.employeeName}</span>
          <span className="text-xs tabular-nums text-muted-foreground">
            Ca #{row.original.counterShiftId}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("openedAt", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mở ca" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">{formatDateTime(row.original.openedAt)}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("closedAt", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Đóng ca" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">{formatDateTime(row.original.closedAt)}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("openingCash", {
      header: () => <div className="text-right">Tiền đầu ca</div>,
      cell: ({ row }) => (
        <div className="text-right text-sm tabular-nums">{money(row.original.openingCash)}</div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("expectedCash", {
      header: () => <div className="text-right">Phải có</div>,
      cell: ({ row }) => (
        <div className="text-right text-sm tabular-nums">{money(row.original.expectedCash)}</div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("countedCash", {
      header: () => <div className="text-right">Thực đếm</div>,
      cell: ({ row }) => (
        <div className="text-right text-sm tabular-nums">{money(row.original.countedCash)}</div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("variance", {
      header: () => <div className="text-right">Chênh lệch</div>,
      cell: ({ row }) => {
        const variance = row.original.variance;
        return (
          <div
            className={cn(
              "text-right text-sm font-medium tabular-nums",
              variance == null && "text-muted-foreground",
              variance === 0 && "text-success",
              variance != null && variance !== 0 && "text-warning-text",
            )}
          >
            {money(variance)}
          </div>
        );
      },
      enableSorting: false,
    }),
    columnHelper.accessor("status", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Trạng thái" />,
      cell: ({ row }) => <ShiftStatusBadge status={row.original.status} />,
      enableSorting: false,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          {row.original.status === COUNTER_SHIFT_STATUS_OPEN ? (
            <Button variant="outline" size="sm" onClick={() => onCloseByAdmin(row.original)}>
              <DoorClosed className="size-4" />
              <span className="hidden sm:inline">Đóng thay ca</span>
            </Button>
          ) : null}
        </div>
      ),
    }),
  ]);
