import { createColumnHelper } from "@tanstack/react-table";
import { IdCard, MoreHorizontal, Pencil, Trash2 } from "lucide-react";

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
import { STATUS_LABEL } from "@/config/default-constants";

import type { TicketTypeDto } from "../types";
import { formatDaysOfWeek, formatMultiplier } from "../utils/pricing-labels";

interface TicketTypeColumnsOptions {
  ageRatingCodes: Map<number, string>;
  onEdit: (ticketType: TicketTypeDto) => void;
  onDelete: (ticketType: TicketTypeDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, TicketTypeDto>();

export const createTicketTypeColumns = ({
  ageRatingCodes,
  onEdit,
  onDelete,
}: TicketTypeColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("ticketTypeId", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="ID" />,
      cell: ({ row }) => (
        <span className="text-muted-foreground w-14 inline-block text-sm tabular-nums">
          #{row.original.ticketTypeId}
        </span>
      ),
    }),
    columnHelper.accessor("name", {
      filterFn: "includesString",
      header: ({ column }) => <DataTableColumnHeader column={column} title="Loại vé" />,
      cell: ({ row }) => <span className="font-medium text-foreground">{row.original.name}</span>,
    }),
    columnHelper.accessor("priceMultiplier", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Hệ số giá" />,
      cell: ({ row }) => (
        <span className="font-semibold tabular-nums text-primary-text">
          {formatMultiplier(row.original.priceMultiplier)}
        </span>
      ),
    }),
    columnHelper.display({
      id: "conditions",
      header: "Điều kiện",
      cell: ({ row }) => {
        const t = row.original;
        const ageCodes = t.allowedAgeRatingIds.map((id) => ageRatingCodes.get(id) ?? `#${id}`);
        const hasWindow = Boolean(t.allowedFromTime && t.allowedToTime);
        const hasDays = t.allowedDaysOfWeek.length > 0;
        const isUnrestricted =
          !t.requiresCardVerification && ageCodes.length === 0 && !hasWindow && !hasDays;

        if (isUnrestricted) {
          return <span className="text-sm text-muted-foreground">Không giới hạn</span>;
        }

        return (
          <div className="flex flex-wrap gap-1">
            {t.requiresCardVerification && (
              <Badge variant="outline">
                <IdCard className="size-3" />
                Xuất trình thẻ
              </Badge>
            )}
            {ageCodes.length > 0 && <Badge variant="secondary">Phim {ageCodes.join(", ")}</Badge>}
            {hasWindow && (
              <Badge variant="secondary">
                {t.allowedFromTime} - {t.allowedToTime}
              </Badge>
            )}
            {hasDays && <Badge variant="secondary">{formatDaysOfWeek(t.allowedDaysOfWeek)}</Badge>}
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
