import { createColumnHelper } from "@tanstack/react-table";
import { MoreHorizontal, Pencil } from "lucide-react";
import { Link } from "react-router";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { ROUTES } from "@/config/routes";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import type { ReconciliationItemDto } from "../types";
import {
  RESOLUTION_BADGE,
  getGatewayLabel,
  getItemResultLabel,
} from "../utils/reconciliation-labels";

const columnHelper = createColumnHelper<DataTableFeatures, ReconciliationItemDto>();

interface ItemColumnsOptions {
  onEdit: (item: ReconciliationItemDto) => void;
}

export const createItemColumns = ({ onEdit }: ItemColumnsOptions) =>
  columnHelper.columns([
    columnHelper.display({
      id: "transaction",
      header: "Giao dịch",
      cell: ({ row }) => {
        const i = row.original;
        return (
          <div className="flex flex-col text-sm">
            <span className="font-medium">{i.gatewayTransactionCode}</span>
            <span className="text-xs text-muted-foreground">
              {getGatewayLabel(i.gateway)} ·{" "}
              <Link
                to={ROUTES.admin.reconciliationDetail(i.sessionId)}
                className="text-primary-text hover:underline"
              >
                Phiên #{i.sessionId}
              </Link>
            </span>
            {i.orderId !== null && (
              <Link
                to={ROUTES.admin.orderDetail(i.orderId)}
                className="text-xs text-primary-text hover:underline"
              >
                Đơn {i.orderCode ?? `#${i.orderId}`}
              </Link>
            )}
          </div>
        );
      },
    }),
    columnHelper.accessor("result", {
      header: "Loại kết quả",
      cell: ({ row }) => <Badge variant="warning">{getItemResultLabel(row.original.result)}</Badge>,
      enableSorting: false,
    }),
    columnHelper.display({
      id: "amount",
      header: "Số tiền (cổng / hệ thống)",
      cell: ({ row }) => (
        <div className="text-sm tabular-nums">
          <p>{row.original.gatewayAmount !== null ? formatCurrency(row.original.gatewayAmount) : "—"}</p>
          <p className="text-xs text-muted-foreground">
            {row.original.systemAmount !== null ? formatCurrency(row.original.systemAmount) : "—"}
          </p>
        </div>
      ),
    }),
    columnHelper.accessor("resolution", {
      header: "Xử lý",
      cell: ({ row }) => {
        const i = row.original;
        const badge = RESOLUTION_BADGE[i.resolution];
        return (
          <div className="flex flex-col gap-1">
            <Badge variant={badge?.variant ?? "outline"} className="w-fit">
              {badge?.label ?? i.resolution}
            </Badge>
            {i.resolvedAt && (
              <span className="text-xs text-muted-foreground">
                {i.resolvedByName ?? "—"} · {formatDateTime(i.resolvedAt)}
              </span>
            )}
          </div>
        );
      },
      enableSorting: false,
    }),
    columnHelper.accessor("assignedToName", {
      header: "Người xử lý",
      cell: ({ row }) => (
        <span className="text-sm">{row.original.assignedToName ?? "—"}</span>
      ),
      enableSorting: false,
    }),
    columnHelper.accessor("note", {
      header: "Ghi chú",
      cell: ({ row }) => (
        <span className="line-clamp-2 max-w-64 text-sm text-muted-foreground">
          {row.original.note ?? "—"}
        </span>
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
                <Pencil /> Xử lý dòng
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ),
    }),
  ]);
