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
import { formatCurrency } from "@/lib/formatters";
import { getPeriodLabel } from "@/features/client/memberships/utils/membership-plans";

import type { AdminMembershipPlanDto } from "../types";
import { describePlanBenefits } from "../utils/membership-plan-admin-labels";

interface MembershipPlanColumnsOptions {
  onEdit: (plan: AdminMembershipPlanDto) => void;
  onDelete: (plan: AdminMembershipPlanDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, AdminMembershipPlanDto>();

export const createMembershipPlanColumns = ({ onEdit, onDelete }: MembershipPlanColumnsOptions) =>
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
      id: "period",
      header: "Thời hạn",
      cell: ({ row }) => (
        <span className="text-sm">
          {getPeriodLabel(row.original.period)} · {row.original.durationMonths} tháng
        </span>
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
      id: "benefits",
      header: "Quyền lợi",
      cell: ({ row }) => {
        const benefits = describePlanBenefits(row.original);
        return (
          <ul className="flex max-w-60 flex-col gap-0.5 text-sm">
            {benefits.length > 0 ? (
              benefits.map((benefit) => <li key={benefit}>{benefit}</li>)
            ) : (
              <li className="text-muted-foreground">Không có</li>
            )}
            <li className="text-xs text-muted-foreground">
              {row.original.stackableWithPromotions
                ? "Dùng chung mã khuyến mãi"
                : "Không dùng chung mã khuyến mãi"}
            </li>
          </ul>
        );
      },
      enableSorting: false,
    }),
    columnHelper.display({
      id: "conversion",
      header: "Quy đổi vé miễn phí",
      cell: ({ row }) =>
        row.original.freeTicketsPerMonth > 0 ? (
          <span className="text-sm tabular-nums">
            {formatCurrency(row.original.freeTicketConversionValue)} / vé
          </span>
        ) : (
          <span className="text-sm text-muted-foreground">-</span>
        ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "status",
      header: () => STATUS_LABEL,
      cell: ({ row }) => (
        <Badge variant={row.original.isActive ? "success" : "secondary"}>
          {row.original.isActive ? "Đang bán" : "Tạm tắt"}
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
