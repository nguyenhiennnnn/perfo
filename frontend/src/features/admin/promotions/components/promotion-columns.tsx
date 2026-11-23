import { createColumnHelper } from "@tanstack/react-table";
import {
  BarChart3,
  History,
  MoreHorizontal,
  Pause,
  Pencil,
  Play,
  Trash2,
} from "lucide-react";

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
import { describePromotionBenefit } from "@/features/client/promotions/utils/promotion-labels";
import { formatDate } from "@/lib/formatters";
import { STATUS_LABEL } from "@/config/default-constants";

import type { PromotionDto } from "../types";
import {
  derivePromotionStatus,
  getPromotionTypeLabel,
  type PromotionConsumption,
} from "../utils/promotion-admin-labels";
import { PromotionStatusBadge } from "./promotion-status-badge";
import { UsageProgress } from "./usage-progress";

interface PromotionColumnsOptions {
  usageMap?: Record<number, PromotionConsumption>;
  onEdit: (promotion: PromotionDto) => void;
  onToggleActive: (promotion: PromotionDto) => void;
  onDelete: (promotion: PromotionDto) => void;
  onViewRedemptions: (promotion: PromotionDto) => void;
  onViewReport: (promotion: PromotionDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, PromotionDto>();

const formatValidity = (promotion: PromotionDto) => {
  if (!promotion.startsAt && !promotion.endsAt) return "Không giới hạn";
  if (promotion.startsAt && promotion.endsAt) {
    return `${formatDate(promotion.startsAt)} - ${formatDate(promotion.endsAt)}`;
  }
  return promotion.startsAt
    ? `Từ ${formatDate(promotion.startsAt)}`
    : `Đến ${formatDate(promotion.endsAt)}`;
};

export const createPromotionColumns = ({
  usageMap,
  onEdit,
  onToggleActive,
  onDelete,
  onViewRedemptions,
  onViewReport,
}: PromotionColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("code", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mã" />,
      cell: ({ row }) => <span className="t-code font-medium">{row.original.code}</span>,
      enableSorting: false,
    }),
    columnHelper.accessor("name", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Khuyến mãi" />,
      cell: ({ row }) => (
        <div className="flex max-w-64 flex-col gap-0.5">
          <span className="truncate font-medium text-foreground">{row.original.name}</span>
          {row.original.assignedUserName && (
            <span className="truncate text-xs text-muted-foreground">
              Cấp cho {row.original.assignedUserName}
            </span>
          )}
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("type", {
      header: "Loại",
      cell: ({ row }) => (
        <span className="text-sm">{getPromotionTypeLabel(row.original.type)}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "benefit",
      header: "Ưu đãi",
      cell: ({ row }) => (
        <span className="text-sm font-semibold text-primary-text">
          {describePromotionBenefit(row.original)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "validity",
      header: "Hiệu lực",
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatValidity(row.original)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "usage",
      header: "Mức dùng",
      cell: ({ row }) => {
        const consumption = usageMap?.[row.original.promotionId];
        if (!consumption) return <span className="text-sm text-muted-foreground">—</span>;
        const limit = row.original.totalUsageLimit;
        return (
          <UsageProgress
            used={consumption.consumedCount}
            limit={limit}
            ratio={limit ? consumption.consumedCount / limit : null}
          />
        );
      },
      enableSorting: false,
    }),
    columnHelper.display({
      id: "status",
      header: () => STATUS_LABEL,
      cell: ({ row }) => (
        <PromotionStatusBadge
          status={derivePromotionStatus(row.original, usageMap?.[row.original.promotionId])}
        />
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
              <DropdownMenuItem onClick={() => onToggleActive(row.original)}>
                {row.original.isActive ? (
                  <>
                    <Pause /> Tạm dừng
                  </>
                ) : (
                  <>
                    <Play /> Tiếp tục
                  </>
                )}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => onViewRedemptions(row.original)}>
                <History /> Lịch sử dùng
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => onViewReport(row.original)}>
                <BarChart3 /> Xem báo cáo
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
