import { createColumnHelper } from "@tanstack/react-table";

import { Badge } from "@/components/ui/badge";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

import type { StockLedgerEntryDto } from "../types";
import {
  formatLedgerReason,
  formatLedgerReference,
  formatQuantity,
  formatSignedQuantity,
  getMovementLabel,
  getMovementVariant,
  getSignedTextClass,
} from "../utils/inventory-labels";

const columnHelper = createColumnHelper<DataTableFeatures, StockLedgerEntryDto>();

export const createStockLedgerColumns = (unit: string) =>
  columnHelper.columns([
    columnHelper.display({
      id: "occurredAt",
      header: "Thời điểm",
      cell: ({ row }) => <span className="tabular-nums">{formatDateTime(row.original.occurredAt)}</span>,
    }),
    columnHelper.display({
      id: "movementType",
      header: "Loại biến động",
      cell: ({ row }) => (
        <Badge variant={getMovementVariant(row.original.movementType)}>
          {getMovementLabel(row.original.movementType)}
        </Badge>
      ),
    }),
    columnHelper.display({
      id: "change",
      header: "Số lượng",
      cell: ({ row }) => (
        <span className={`font-medium tabular-nums ${getSignedTextClass(row.original.quantityChange)}`}>
          {formatSignedQuantity(row.original.quantityChange, unit)}
        </span>
      ),
    }),
    columnHelper.display({
      id: "balance",
      header: "Tồn sau biến động",
      cell: ({ row }) => (
        <span className="tabular-nums">{formatQuantity(row.original.balanceAfter, unit)}</span>
      ),
    }),
    columnHelper.display({
      id: "unitCost",
      header: "Giá vốn",
      cell: ({ row }) => <span className="tabular-nums">{formatCurrency(row.original.unitCost)}</span>,
    }),
    columnHelper.display({
      id: "reference",
      header: "Chứng từ",
      cell: ({ row }) => (
        <span className="text-muted-foreground">
          {formatLedgerReference(row.original.referenceType, row.original.referenceId) ?? "—"}
        </span>
      ),
    }),
    columnHelper.display({
      id: "reason",
      header: "Lý do",
      cell: ({ row }) => (
        <span className="text-muted-foreground">
          {formatLedgerReason(row.original.movementType, row.original.reason) ?? "—"}
        </span>
      ),
    }),
    columnHelper.display({
      id: "actor",
      header: "Người thực hiện",
      cell: ({ row }) => <span>{row.original.actorName ?? "Hệ thống"}</span>,
    }),
  ]);
