import { createColumnHelper } from "@tanstack/react-table";
import { History, MoreHorizontal, Pencil, Trash2 } from "lucide-react";

import { ActiveStatusBadge } from "@/components/common/status-badge";
import { Badge } from "@/components/ui/badge";
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

import type { PricingRuleDto } from "../types";
import {
  formatDaysOfWeek,
  formatRuleAdjustment,
  getRuleTypeLabel,
} from "../utils/pricing-labels";

interface PricingRuleColumnsOptions {
  specialDayNames: Map<number, string>;
  onEdit: (rule: PricingRuleDto) => void;
  onHistory: (rule: PricingRuleDto) => void;
  onDelete: (rule: PricingRuleDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, PricingRuleDto>();

const summarizeCondition = (rule: PricingRuleDto, specialDayNames: Map<number, string>) => {
  const { condition } = rule;
  switch (rule.ruleType) {
    case "TimeOfDay":
      return `${condition.startTime ?? ""} - ${condition.endTime ?? ""}`;
    case "WeekDay":
      return formatDaysOfWeek(condition.daysOfWeek ?? []);
    case "SpecialDay": {
      const ids = condition.specialDayIds ?? [];
      if (ids.length === 0) return "Mọi ngày lễ đang hoạt động";
      return ids.map((id) => specialDayNames.get(id) ?? `#${id}`).join(", ");
    }
    default: {
      const count = condition.tiers?.length ?? 0;
      return `${count} bậc lấp đầy`;
    }
  }
};

const summarizeAdjustment = (rule: PricingRuleDto) => {
  if (rule.ruleType === "Occupancy") {
    const tiers = rule.condition.tiers ?? [];
    return tiers.map((tier) => formatRuleAdjustment(rule.adjustmentMode, tier.value)).join(" / ");
  }
  return formatRuleAdjustment(rule.adjustmentMode, rule.adjustmentValue ?? 0);
};

const formatValidity = (rule: PricingRuleDto) => {
  if (!rule.validFrom && !rule.validTo) return "Không giới hạn";
  if (rule.validFrom && rule.validTo) return `${formatDate(rule.validFrom)} - ${formatDate(rule.validTo)}`;
  return rule.validFrom ? `Từ ${formatDate(rule.validFrom)}` : `Đến ${formatDate(rule.validTo)}`;
};

export const createPricingRuleColumns = ({
  specialDayNames,
  onEdit,
  onHistory,
  onDelete,
}: PricingRuleColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("pricingRuleId", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="ID" />,
      cell: ({ row }) => (
        <span className="text-muted-foreground w-14 inline-block text-sm tabular-nums">
          #{row.original.pricingRuleId}
        </span>
      ),
    }),
    columnHelper.accessor("name", {
      filterFn: "includesString",
      header: ({ column }) => <DataTableColumnHeader column={column} title="Quy tắc" />,
      cell: ({ row }) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-medium text-foreground">{row.original.name}</span>
          <span className="text-xs text-muted-foreground">
            {getRuleTypeLabel(row.original.ruleType)} · {summarizeCondition(row.original, specialDayNames)}
          </span>
        </div>
      ),
    }),
    columnHelper.display({
      id: "adjustment",
      header: "Điều chỉnh",
      cell: ({ row }) => (
        <span className="font-semibold tabular-nums text-primary-text">
          {summarizeAdjustment(row.original)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("priority", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Ưu tiên" />,
      cell: ({ row }) => <span className="tabular-nums">{row.original.priority}</span>,
    }),
    columnHelper.display({
      id: "scope",
      header: "Phạm vi",
      cell: ({ row }) => {
        const { roomIds, screenTypeIds, movieIds } = row.original;
        if (roomIds.length === 0 && screenTypeIds.length === 0 && movieIds.length === 0) {
          return <span className="text-sm text-muted-foreground">Tất cả</span>;
        }
        return (
          <div className="flex flex-wrap gap-1">
            {roomIds.length > 0 && <Badge variant="secondary">{roomIds.length} phòng</Badge>}
            {screenTypeIds.length > 0 && (
              <Badge variant="secondary">{screenTypeIds.length} định dạng</Badge>
            )}
            {movieIds.length > 0 && <Badge variant="secondary">{movieIds.length} phim</Badge>}
          </div>
        );
      },
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
              <DropdownMenuItem onClick={() => onHistory(row.original)}>
                <History /> Lịch sử thay đổi
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
