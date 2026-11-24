import { createColumnHelper } from "@tanstack/react-table";

import { Button } from "@/components/ui/button";
import { DataTableColumnHeader } from "@/components/common/data-table/data-table-column-header";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { STATUS_LABEL } from "@/config/default-constants";

import type { StockLevelDto } from "../types";
import { formatQuantity } from "../utils/inventory-labels";
import { StockStatusBadge } from "./stock-status-badge";

const columnHelper = createColumnHelper<DataTableFeatures, StockLevelDto>();

export const createStockLevelColumns = (onViewLedger: (level: StockLevelDto) => void) =>
  columnHelper.columns([
    columnHelper.accessor("code", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Mã" />,
      cell: ({ row }) => <span className="t-code">{row.original.code}</span>,
      enableSorting: false,
    }),
    columnHelper.accessor("name", {
      header: ({ column }) => <DataTableColumnHeader column={column} title="Sản phẩm kho" />,
      cell: ({ row }) => <span className="font-medium text-foreground">{row.original.name}</span>,
      enableSorting: false,
    }),
    columnHelper.display({
      id: "onHand",
      header: "Tồn sổ sách",
      cell: ({ row }) => (
        <span className="tabular-nums">{formatQuantity(row.original.quantityOnHand, row.original.unit)}</span>
      ),
    }),
    columnHelper.display({
      id: "reserved",
      header: "Đang giữ chỗ",
      cell: ({ row }) => (
        <span className="tabular-nums text-muted-foreground">
          {formatQuantity(row.original.reservedQuantity, row.original.unit)}
        </span>
      ),
    }),
    columnHelper.display({
      id: "available",
      header: "Khả dụng",
      cell: ({ row }) => (
        <span className="font-medium tabular-nums">
          {formatQuantity(row.original.availableQuantity, row.original.unit)}
        </span>
      ),
    }),
    columnHelper.display({
      id: "threshold",
      header: "Ngưỡng cảnh báo",
      cell: ({ row }) => (
        <span className="tabular-nums">{formatQuantity(row.original.lowStockThreshold, row.original.unit)}</span>
      ),
    }),
    columnHelper.display({
      id: "status",
      header: STATUS_LABEL,
      cell: ({ row }) => (
        <StockStatusBadge
          availableQuantity={row.original.availableQuantity}
          isLowStock={row.original.isLowStock}
        />
      ),
    }),
    columnHelper.display({
      id: "actions",
      header: () => <div className="text-right">Hành động</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          <Button variant="ghost" size="sm" onClick={() => onViewLedger(row.original)}>
            Xem sổ cái
          </Button>
        </div>
      ),
      enableHiding: false,
    }),
  ]);
