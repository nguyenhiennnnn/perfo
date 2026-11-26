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
import { formatCurrency, formatNumber } from "@/lib/formatters";

import type { AdminBundleDto } from "../types";
import {
  BUNDLE_AVAILABILITY_BADGES,
  deriveBundleAvailability,
  describeBundleItem,
  describeBundleValidity,
} from "../utils/bundle-admin-labels";

interface BundleColumnsOptions {
  onEdit: (bundle: AdminBundleDto) => void;
  onDelete: (bundle: AdminBundleDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, AdminBundleDto>();

export const createBundleColumns = ({ onEdit, onDelete }: BundleColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("name", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Gói" />,
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
      id: "items",
      header: "Thành phần",
      cell: ({ row }) => (
        <ul className="flex max-w-64 flex-col gap-0.5 text-sm">
          {row.original.items.map((item) => (
            <li key={item.bundleItemId} className="truncate">
              {describeBundleItem(item)}
            </li>
          ))}
        </ul>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("price", {
      header: "Giá gói",
      cell: ({ row }) => (
        <span className="font-semibold tabular-nums text-primary-text">
          {formatCurrency(row.original.price)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "conditions",
      header: "Điều kiện",
      cell: ({ row }) => {
        const bundle = row.original;
        const scopes = [
          bundle.movieIds.length > 0 ? `${bundle.movieIds.length} phim` : null,
          bundle.showtimeIds.length > 0 ? `${bundle.showtimeIds.length} suất` : null,
          bundle.allowedFromTime && bundle.allowedToTime
            ? `${bundle.allowedFromTime} - ${bundle.allowedToTime}`
            : null,
          bundle.daysOfWeek.length > 0 ? `${bundle.daysOfWeek.length} thứ` : null,
        ].filter(Boolean);
        return (
          <div className="flex max-w-56 flex-col gap-0.5 text-xs text-muted-foreground">
            <span>{scopes.length > 0 ? scopes.join(" · ") : "Mọi suất chiếu"}</span>
            {bundle.excludesPromotions && <span>Không dùng chung mã khuyến mãi</span>}
          </div>
        );
      },
      enableSorting: false,
    }),
    columnHelper.display({
      id: "dailyLimit",
      header: "Hạn mức/ngày",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">
          {row.original.dailyLimit === null ? "Không giới hạn" : formatNumber(row.original.dailyLimit)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "validity",
      header: "Hiệu lực",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {describeBundleValidity(row.original)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "status",
      header: () => STATUS_LABEL,
      cell: ({ row }) => {
        const item = BUNDLE_AVAILABILITY_BADGES[deriveBundleAvailability(row.original)];
        return <Badge variant={item.variant}>{item.label}</Badge>;
      },
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
