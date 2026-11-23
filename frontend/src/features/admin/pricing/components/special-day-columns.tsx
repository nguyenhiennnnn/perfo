import { createColumnHelper } from "@tanstack/react-table";
import { MoreHorizontal, Pencil, Trash2 } from "lucide-react";

import { ActiveStatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatDate } from "@/lib/formatters";
import { STATUS_LABEL } from "@/config/default-constants";

import type { SpecialDayDto } from "../types";

interface SpecialDayColumnsOptions {
  onEdit: (specialDay: SpecialDayDto) => void;
  onDelete: (specialDay: SpecialDayDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, SpecialDayDto>();

const MS_PER_DAY = 24 * 60 * 60 * 1000;

const countDays = (startDate: string, endDate: string) =>
  Math.round((Date.parse(endDate.slice(0, 10)) - Date.parse(startDate.slice(0, 10))) / MS_PER_DAY) + 1;

export const createSpecialDayColumns = ({ onEdit, onDelete }: SpecialDayColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("specialDayId", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="ID" />,
      cell: ({ row }) => (
        <span className="text-muted-foreground w-14 inline-block text-sm tabular-nums">
          #{row.original.specialDayId}
        </span>
      ),
    }),
    columnHelper.accessor("name", {
      filterFn: "includesString",
      header: ({ column }) => <DataTableColumnHeader column={column} title="Ngày lễ / sự kiện" />,
      cell: ({ row }) => <span className="font-medium text-foreground">{row.original.name}</span>,
    }),
    columnHelper.display({
      id: "range",
      header: "Thời gian",
      cell: ({ row }) => {
        const { startDate, endDate } = row.original;
        const isSingle = startDate === endDate;
        return (
          <div className="flex flex-col">
            <span className="tabular-nums">
              {isSingle ? formatDate(startDate) : `${formatDate(startDate)} - ${formatDate(endDate)}`}
            </span>
            <span className="text-xs text-muted-foreground">
              {countDays(startDate, endDate)} ngày
            </span>
          </div>
        );
      },
      enableSorting: false,
    }),
    columnHelper.accessor("isActive", {
      header: ({ column }) => <DataTableColumnHeader column={column} title={STATUS_LABEL} />,
      cell: ({ row }) => <ActiveStatusBadge active={row.original.isActive} />,
      enableSorting: false,
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
              <DropdownMenuItem onClick={() => onEdit(row.original)}>
                <Pencil /> Chỉnh sửa
              </DropdownMenuItem>
              <DropdownMenuItem variant="destructive" onClick={() => onDelete(row.original)}>
                <Trash2 /> Xóa
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ),
      enableHiding: false,
    }),
  ]);
