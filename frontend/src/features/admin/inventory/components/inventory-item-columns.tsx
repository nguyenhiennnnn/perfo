import { createColumnHelper } from "@tanstack/react-table";
import { BookOpen, Pencil, Trash2 } from "lucide-react";

import { ActiveStatusBadge } from "@/components/common/status-badge";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatCurrency } from "@/lib/formatters";
import { STATUS_LABEL } from "@/config/default-constants";

import type { InventoryItemDto } from "../types";
import { formatQuantity } from "../utils/inventory-labels";
import { RowActionsMenu } from "./row-actions-menu";
import { StockStatusBadge } from "./stock-status-badge";

interface InventoryItemColumnsOptions {
  onEdit: (item: InventoryItemDto) => void;
  onDelete: (item: InventoryItemDto) => void;
  onViewLedger: (item: InventoryItemDto) => void;
}

const columnHelper = createColumnHelper<DataTableFeatures, InventoryItemDto>();

export const createInventoryItemColumns = ({
  onEdit,
  onDelete,
  onViewLedger,
}: InventoryItemColumnsOptions) =>
  columnHelper.columns([
    columnHelper.accessor("code", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mã" />,
      cell: ({ row }) => <span className="t-code">{row.original.code}</span>,
      enableSorting: false,
    }),
    columnHelper.accessor("name", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Sản phẩm kho" />,
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="font-medium text-foreground">{row.original.name}</span>
          <span className="text-xs text-muted-foreground">Đơn vị: {row.original.unit}</span>
        </div>
      ),
      enableSorting: false,
    }),
    columnHelper.display({
      id: "onHand",
      header: "Tồn sổ sách",
      cell: ({ row }) => (
        <div className="flex flex-col tabular-nums">
          <span>{formatQuantity(row.original.quantityOnHand, row.original.unit)}</span>
          {row.original.reservedQuantity > 0 && (
            <span className="text-xs text-muted-foreground">
              Đang giữ chỗ {formatQuantity(row.original.reservedQuantity)}
            </span>
          )}
        </div>
      ),
    }),
    columnHelper.display({
      id: "unitCost",
      header: "Giá vốn",
      cell: ({ row }) => <span className="tabular-nums">{formatCurrency(row.original.unitCost)}</span>,
    }),
    columnHelper.display({
      id: "stockValue",
      header: "Giá trị tồn",
      cell: ({ row }) => <span className="tabular-nums">{formatCurrency(row.original.stockValue)}</span>,
    }),
    columnHelper.display({
      id: "threshold",
      header: "Ngưỡng cảnh báo",
      cell: ({ row }) => (
        <span className="tabular-nums">
          {formatQuantity(row.original.lowStockThreshold, row.original.unit)}
        </span>
      ),
    }),
    columnHelper.display({
      id: "status",
      header: STATUS_LABEL,
      cell: ({ row }) => (
        <div className="flex flex-wrap gap-1">
          <ActiveStatusBadge active={row.original.isActive} />
          {row.original.isActive && (
            <StockStatusBadge
              availableQuantity={row.original.availableQuantity}
              isLowStock={row.original.isLowStock}
            />
          )}
        </div>
      ),
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => (
        <RowActionsMenu
          actions={[
            { label: "Chỉnh sửa", icon: <Pencil />, onClick: () => onEdit(row.original) },
            { label: "Xem sổ cái", icon: <BookOpen />, onClick: () => onViewLedger(row.original) },
            {
              label: "Xóa",
              icon: <Trash2 />,
              onClick: () => onDelete(row.original),
              destructive: true,
            },
          ]}
        />
      ),
      enableHiding: false,
    }),
  ]);
