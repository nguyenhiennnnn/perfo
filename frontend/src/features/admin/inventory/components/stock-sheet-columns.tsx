import { createColumnHelper } from "@tanstack/react-table";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { DataTableFeatures } from "@/components/common/data-table/data-table-features";
import { formatCurrency, formatDate, formatDateTime } from "@/lib/formatters";

import type {
  StockReceiptListItemDto,
  StocktakeSheetListItemDto,
  WasteSheetListItemDto,
} from "../types";
import {
  formatSignedQuantity,
  getSignedTextClass,
  getWasteReasonLabel,
} from "../utils/inventory-labels";

const ViewButton = ({ onClick }: { onClick: () => void }) => (
  <div className="flex justify-end">
    <Button variant="ghost" size="sm" onClick={onClick}>
      Xem chi tiết
    </Button>
  </div>
);

const actionsHeader = () => <div className="text-right">Hành động</div>;

const receiptHelper = createColumnHelper<DataTableFeatures, StockReceiptListItemDto>();

export const createReceiptColumns = (onView: (id: number) => void) =>
  receiptHelper.columns([
    receiptHelper.display({
      id: "code",
      header: "Phiếu",
      cell: ({ row }) => <span className="t-code">#{row.original.stockReceiptId}</span>,
    }),
    receiptHelper.display({
      id: "receivedDate",
      header: "Ngày nhập",
      cell: ({ row }) => <span className="tabular-nums">{formatDate(row.original.receivedDate)}</span>,
    }),
    receiptHelper.display({
      id: "supplier",
      header: "Nhà cung cấp",
      cell: ({ row }) => <span className="font-medium text-foreground">{row.original.supplierName}</span>,
    }),
    receiptHelper.display({
      id: "lineCount",
      header: "Số dòng",
      cell: ({ row }) => <span className="tabular-nums">{row.original.lineCount}</span>,
    }),
    receiptHelper.display({
      id: "total",
      header: "Tổng tiền",
      cell: ({ row }) => <span className="tabular-nums">{formatCurrency(row.original.totalAmount)}</span>,
    }),
    receiptHelper.display({
      id: "createdAt",
      header: "Tạo lúc",
      cell: ({ row }) => (
        <span className="tabular-nums text-muted-foreground">{formatDateTime(row.original.createdAt)}</span>
      ),
    }),
    receiptHelper.display({
      id: "actions",
      header: actionsHeader,
      cell: ({ row }) => <ViewButton onClick={() => onView(row.original.stockReceiptId)} />,
      enableHiding: false,
    }),
  ]);

const stocktakeHelper = createColumnHelper<DataTableFeatures, StocktakeSheetListItemDto>();

export const createStocktakeColumns = (onView: (id: number) => void) =>
  stocktakeHelper.columns([
    stocktakeHelper.display({
      id: "code",
      header: "Phiếu",
      cell: ({ row }) => <span className="t-code">#{row.original.stocktakeSheetId}</span>,
    }),
    stocktakeHelper.display({
      id: "stocktakeDate",
      header: "Ngày kiểm kê",
      cell: ({ row }) => <span className="tabular-nums">{formatDate(row.original.stocktakeDate)}</span>,
    }),
    stocktakeHelper.display({
      id: "note",
      header: "Ghi chú",
      cell: ({ row }) => <span className="text-muted-foreground">{row.original.note || "—"}</span>,
    }),
    stocktakeHelper.display({
      id: "lineCount",
      header: "Số dòng",
      cell: ({ row }) => <span className="tabular-nums">{row.original.lineCount}</span>,
    }),
    stocktakeHelper.display({
      id: "difference",
      header: "Tổng chênh lệch",
      cell: ({ row }) => (
        <span className={`tabular-nums ${getSignedTextClass(row.original.totalDifference)}`}>
          {formatSignedQuantity(row.original.totalDifference)}
        </span>
      ),
    }),
    stocktakeHelper.display({
      id: "actions",
      header: actionsHeader,
      cell: ({ row }) => <ViewButton onClick={() => onView(row.original.stocktakeSheetId)} />,
      enableHiding: false,
    }),
  ]);

const wasteHelper = createColumnHelper<DataTableFeatures, WasteSheetListItemDto>();

export const createWasteColumns = (onView: (id: number) => void) =>
  wasteHelper.columns([
    wasteHelper.display({
      id: "code",
      header: "Phiếu",
      cell: ({ row }) => <span className="t-code">#{row.original.wasteSheetId}</span>,
    }),
    wasteHelper.display({
      id: "wasteDate",
      header: "Ngày hủy hàng",
      cell: ({ row }) => <span className="tabular-nums">{formatDate(row.original.wasteDate)}</span>,
    }),
    wasteHelper.display({
      id: "reason",
      header: "Lý do",
      cell: ({ row }) => <Badge variant="outline">{getWasteReasonLabel(row.original.reason)}</Badge>,
    }),
    wasteHelper.display({
      id: "lineCount",
      header: "Số dòng",
      cell: ({ row }) => <span className="tabular-nums">{row.original.lineCount}</span>,
    }),
    wasteHelper.display({
      id: "total",
      header: "Giá trị hao hụt",
      cell: ({ row }) => <span className="tabular-nums">{formatCurrency(row.original.totalAmount)}</span>,
    }),
    wasteHelper.display({
      id: "actions",
      header: actionsHeader,
      cell: ({ row }) => <ViewButton onClick={() => onView(row.original.wasteSheetId)} />,
      enableHiding: false,
    }),
  ]);
