import { createColumnHelper } from "@tanstack/react-table";
import { Eye } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatDateTime } from "@/lib/formatters";

import type { AuditLogListItemDto } from "@/features/admin/audit-logs/types";
import {
  SYSTEM_ACTOR_LABEL,
  getAuditActionLabel,
  getAuditActionVariant,
  getAuditEntityTypeLabel,
} from "../utils/audit-log-labels";

interface AuditLogColumnsOptions {
  onViewDetail: (log: AuditLogListItemDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, AuditLogListItemDto>();

export const createAuditLogColumns = ({ onViewDetail }: AuditLogColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("occurredAt", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Thời điểm" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-foreground">
          {formatDateTime(row.original.occurredAt)}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("actorName", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Người thực hiện" />,
      cell: ({ row }) => (
        <span className="text-sm text-foreground">
          {row.original.actorName ?? SYSTEM_ACTOR_LABEL}
        </span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("action", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Thao tác" />,
      cell: ({ row }) => (
        <Badge variant={getAuditActionVariant(row.original.action)}>
          {getAuditActionLabel(row.original.action)}
        </Badge>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("entityType", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Đối tượng" />,
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="text-sm text-foreground">
            {getAuditEntityTypeLabel(row.original.entityType)}
          </span>
          <span className="text-xs tabular-nums text-muted-foreground">
            #{row.original.entityId}
          </span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onViewDetail(row.original)}
            aria-label={`Xem chi tiết nhật ký #${row.original.auditLogId}`}
          >
            <Eye className="size-4" />
            <span className="hidden sm:inline">Chi tiết</span>
          </Button>
        </div>
      ),
    }),
  ]);
