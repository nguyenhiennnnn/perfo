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
import { formatDate, formatPoints } from "@/lib/formatters";

import type { AdminLoyaltyRewardDto } from "../types";
import {
  REWARD_AVAILABILITY_BADGES,
  deriveRewardAvailability,
  describeAdminRewardBenefit,
  getRewardTypeLabel,
} from "../utils/loyalty-admin-labels";

interface RewardColumnsOptions {
  onEdit: (reward: AdminLoyaltyRewardDto) => void;
  onDelete: (reward: AdminLoyaltyRewardDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, AdminLoyaltyRewardDto>();

const formatValidity = (reward: AdminLoyaltyRewardDto) => {
  if (!reward.validFrom && !reward.validTo) return "Không giới hạn";
  if (reward.validFrom && reward.validTo) {
    return `${formatDate(reward.validFrom)} - ${formatDate(reward.validTo)}`;
  }
  return reward.validFrom
    ? `Từ ${formatDate(reward.validFrom)}`
    : `Đến ${formatDate(reward.validTo)}`;
};

export const createRewardColumns = ({ onEdit, onDelete }: RewardColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("name", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Quà" />,
      cell: ({ row }) => (
        <div className="flex max-w-64 flex-col gap-0.5">
          <span className="truncate font-medium text-foreground">{row.original.name}</span>
          <span className="truncate text-xs text-muted-foreground">
            {getRewardTypeLabel(row.original.type)}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "benefit",
      header: "Ưu đãi",
      cell: ({ row }) => (
        <span className="text-sm font-semibold text-primary-text">
          {describeAdminRewardBenefit(row.original)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("pointsCost", {
      header: "Điểm cần",
      cell: ({ row }) => (
        <span className="text-sm font-medium tabular-nums">
          {formatPoints(row.original.pointsCost)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("remainingQuantity", {
      header: "Còn lại",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">{row.original.remainingQuantity}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "validity",
      header: "Thời gian đổi",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatValidity(row.original)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "codeValidity",
      header: "Hạn dùng mã",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {row.original.codeValidityDays} ngày
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "status",
      header: () => STATUS_LABEL,
      cell: ({ row }) => {
        const item = REWARD_AVAILABILITY_BADGES[deriveRewardAvailability(row.original)];
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
