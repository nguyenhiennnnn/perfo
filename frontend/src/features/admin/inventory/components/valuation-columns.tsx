import { createColumnHelper } from "@tanstack/react-table";

import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatCurrency, formatPercent } from "@/lib/formatters";

import type { InventoryValuationItemDto } from "../types";
import { formatQuantity } from "../utils/inventory-labels";

const columnHelper = createColumnHelper<DataTableFeatures, InventoryValuationItemDto>();

export const createValuationColumns = (totalValue: number) =>
  columnHelper.columns([
    columnHelper.display({
      id: "code",
      header: "Mã",
      cell: ({ row }) => <span className="t-code">{row.original.code}</span>,
    }),
    columnHelper.display({
      id: "name",
      header: "Sản phẩm kho",
      cell: ({ row }) => <span className="font-medium text-foreground">{row.original.name}</span>,
    }),
    columnHelper.display({
      id: "quantity",
      header: "Tồn",
      cell: ({ row }) => (
        <span className="tabular-nums">{formatQuantity(row.original.quantityOnHand, row.original.unit)}</span>
      ),
    }),
    columnHelper.display({
      id: "unitCost",
      header: "Giá vốn bình quân",
      cell: ({ row }) => <span className="tabular-nums">{formatCurrency(row.original.unitCost)}</span>,
    }),
    columnHelper.display({
      id: "value",
      header: "Giá trị tồn",
      cell: ({ row }) => (
        <span className="font-medium tabular-nums">{formatCurrency(row.original.stockValue)}</span>
      ),
    }),
    columnHelper.display({
      id: "share",
      header: "Tỷ trọng",
      cell: ({ row }) => (
        <span className="tabular-nums text-muted-foreground">
          {totalValue > 0 ? formatPercent((row.original.stockValue / totalValue) * 100) : "—"}
        </span>
      ),
    }),
  ]);
