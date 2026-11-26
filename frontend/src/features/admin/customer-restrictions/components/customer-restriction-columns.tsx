import { createColumnHelper } from "@tanstack/react-table";
import { CheckCircle2, Clock, ShieldAlert } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { STATUS_LABEL } from "@/config/default-constants";
import { formatDateTime } from "@/lib/formatters";
import { getCustomerRestrictionReasonLabel } from "@/lib/enum-options";
import { CustomerRestrictionStatus } from "@/types/enum";

import type { CustomerRestrictionDto } from "../types";

interface CustomerRestrictionColumnsOptions {
  onLift: (item: CustomerRestrictionDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, CustomerRestrictionDto>();

export const CustomerRestrictionStatusBadge = ({ status }: { status: string }) => {
  if (status === CustomerRestrictionStatus.Active) {
    return (
      <Badge variant="destructive">
        <ShieldAlert className="size-3" />
        Đang hạn chế
      </Badge>
    );
  }
  if (status === CustomerRestrictionStatus.Lifted) {
    return (
      <Badge variant="success">
        <CheckCircle2 className="size-3" />
        Đã gỡ
      </Badge>
    );
  }
  return (
    <Badge variant="secondary">
      <Clock className="size-3" />
      Hết hạn
    </Badge>
  );
};

export const createCustomerRestrictionColumns = ({ onLift }: CustomerRestrictionColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("customerName", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Khách hàng" />,
      cell: ({ row }) => (
        <div className="flex min-w-40 flex-col">
          <span className="font-medium text-foreground">{row.original.customerName}</span>
          <span className="text-xs text-muted-foreground">{row.original.customerEmail}</span>
        </div>
      ),
    }),
    columnHelper.accessor("reason", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Lý do" />,
      cell: ({ row }) => (
        <div className="flex min-w-40 flex-col">
          <span className="text-sm text-foreground">
            {getCustomerRestrictionReasonLabel(row.original.reason)}
          </span>
          <span className="text-xs text-muted-foreground">
            {row.original.expiredOrderCount} đơn hết hạn liên tiếp
          </span>
        </div>
      ),
    }),
    columnHelper.accessor("status", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title={STATUS_LABEL} />,
      cell: ({ row }) => <CustomerRestrictionStatusBadge status={row.original.status} />,
    }),
    columnHelper.accessor("startedAt", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Bắt đầu" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-muted-foreground">
          {formatDateTime(row.original.startedAt)}
        </span>
      ),
    }),
    columnHelper.accessor("expiresAt", {
      enableSorting: false,
      header: ({ column }) => <DataTableColumnHeader column={column} title="Thời hạn đến" />,
      cell: ({ row }) => (
        <span className="text-sm tabular-nums text-foreground">
          {formatDateTime(row.original.expiresAt)}
        </span>
      ),
    }),
    columnHelper.display({
      id: "liftedBy",
      header: "Gỡ bởi",
      cell: ({ row }) =>
        row.original.liftedAt ? (
          <div className="flex min-w-32 flex-col">
            <span className="text-sm text-foreground">{row.original.liftedByName ?? "—"}</span>
            <span className="text-xs tabular-nums text-muted-foreground">
              {formatDateTime(row.original.liftedAt)}
            </span>
          </div>
        ) : (
          <span className="text-sm text-muted-foreground">—</span>
        ),
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) =>
        row.original.status === CustomerRestrictionStatus.Active ? (
          <div className="flex justify-end">
            <Button size="sm" variant="outline" onClick={() => onLift(row.original)}>
              Gỡ hạn chế
            </Button>
          </div>
        ) : null,
      enableHiding: false,
    }),
  ]);
