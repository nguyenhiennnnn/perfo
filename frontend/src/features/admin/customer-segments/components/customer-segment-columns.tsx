import { createColumnHelper } from "@tanstack/react-table";
import { MoreHorizontal, Pencil, Trash2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { STATUS_LABEL } from "@/config/default-constants";
import { formatNumber, formatPercent } from "@/lib/formatters";

import type { CustomerSegmentDto } from "../types";
import { describeSegmentConditions, type SegmentLookups } from "../utils/segment-condition-fields";

interface CustomerSegmentColumnsOptions {
  lookups: SegmentLookups;
  onEdit: (segment: CustomerSegmentDto) => void;
  onDelete: (segment: CustomerSegmentDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, CustomerSegmentDto>();

export const createCustomerSegmentColumns = ({
  lookups,
  onEdit,
  onDelete,
}: CustomerSegmentColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("name", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Phân khúc" />,
      cell: ({ row }) => (
        <div className="flex max-w-64 flex-col gap-0.5">
          <span className="truncate font-medium text-foreground">{row.original.name}</span>
          {row.original.description && (
            <span className="line-clamp-1 text-xs text-muted-foreground">
              {row.original.description}
            </span>
          )}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "conditions",
      header: "Điều kiện",
      cell: ({ row }) => {
        const text = describeSegmentConditions(row.original.conditions, lookups);
        return (
          <p className="line-clamp-2 max-w-96 text-sm text-muted-foreground" title={text}>
            {text}
          </p>
        );
      },
      enableSorting: false,
    }),
    columnHelper.display({
      id: "customerCount",
      header: () => <div className="text-right">Số khách</div>,
      cell: ({ row }) => (
        <div className="text-right font-semibold tabular-nums text-foreground">
          {formatNumber(row.original.customerCount)}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "eligibleCount",
      header: () => <div className="text-right">Đủ điều kiện nhận thông tin</div>,
      cell: ({ row }) => {
        const { customerCount, eligibleCount } = row.original;
        return (
          <div className="flex flex-col items-end gap-0.5">
            <span className="font-semibold tabular-nums text-foreground">
              {formatNumber(eligibleCount)}
            </span>
            {customerCount > 0 && (
              <span className="text-xs tabular-nums text-muted-foreground">
                {formatPercent((eligibleCount * 100) / customerCount)} số khách
              </span>
            )}
          </div>
        );
      },
      enableSorting: false,
    }),
    columnHelper.display({
      id: "status",
      header: () => STATUS_LABEL,
      cell: ({ row }) => (
        <Badge variant={row.original.isActive ? "success" : "secondary"}>
          {row.original.isActive ? "Đang hoạt động" : "Tạm dừng"}
        </Badge>
      ),
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
              <DropdownMenuSeparator />
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
