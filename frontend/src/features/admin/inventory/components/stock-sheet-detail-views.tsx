import type { ReactNode } from "react";
import type { UseQueryResult } from "@tanstack/react-query";

import { Badge } from "@/components/ui/badge";
import { QueryState } from "@/components/common/query-state";
import { FormSkeleton } from "@/components/common/skeleton-shapes";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatCurrency, formatDate, formatDateTime } from "@/lib/formatters";

import {
  useAdminStockReceipt,
  useAdminStocktake,
  useAdminWasteSheet,
} from "../hooks/use-admin-stock-sheets";
import type { StockReceiptDto, StocktakeSheetDto, WasteSheetDto } from "../types";
import {
  formatQuantity,
  formatSignedQuantity,
  getSignedTextClass,
  getWasteReasonLabel,
} from "../utils/inventory-labels";

const MetaRow = ({ label, children }: { label: string; children: ReactNode }) => (
  <div className="flex flex-col gap-0.5">
    <dt className="text-xs text-muted-foreground">{label}</dt>
    <dd className="text-sm font-medium text-foreground">{children}</dd>
  </div>
);

const Meta = ({ children }: { children: ReactNode }) => (
  <dl className="grid grid-cols-2 gap-4 sm:grid-cols-3">{children}</dl>
);

const Right = ({ children }: { children: ReactNode }) => (
  <span className="block text-right tabular-nums">{children}</span>
);

const RightHead = ({ children }: { children: ReactNode }) => (
  <TableHead className="text-right">{children}</TableHead>
);

const ItemCell = ({ code, name }: { code: string; name: string }) => (
  <TableCell>
    <div className="flex flex-col">
      <span className="font-medium text-foreground">{name}</span>
      <span className="t-code text-muted-foreground">{code}</span>
    </div>
  </TableCell>
);

const DetailQuery = <T,>({
  query,
  title,
  children,
}: {
  query: UseQueryResult<T>;
  title: string;
  children: (data: T) => ReactNode;
}) => (
  <QueryState query={query} skeleton={<FormSkeleton fields={3} />} errorTitle={title}>
    {children}
  </QueryState>
);

const ReceiptDetail = ({ receipt }: { receipt: StockReceiptDto }) => (
  <div className="space-y-5">
    <Meta>
      <MetaRow label="Nhà cung cấp">{receipt.supplierName}</MetaRow>
      <MetaRow label="Ngày nhập">{formatDate(receipt.receivedDate)}</MetaRow>
      <MetaRow label="Người lập">{receipt.createdByName ?? "—"}</MetaRow>
      <MetaRow label="Tạo lúc">{formatDateTime(receipt.createdAt)}</MetaRow>
      <MetaRow label="Tổng tiền">{formatCurrency(receipt.totalAmount)}</MetaRow>
      {receipt.note && <MetaRow label="Ghi chú">{receipt.note}</MetaRow>}
    </Meta>
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Sản phẩm kho</TableHead>
          <RightHead>Số lượng</RightHead>
          <RightHead>Đơn giá nhập</RightHead>
          <RightHead>Thành tiền</RightHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {receipt.lines.map((line) => (
          <TableRow key={line.stockReceiptLineId}>
            <ItemCell code={line.itemCode} name={line.itemName} />
            <TableCell><Right>{formatQuantity(line.quantity, line.unit)}</Right></TableCell>
            <TableCell><Right>{formatCurrency(line.unitCost)}</Right></TableCell>
            <TableCell><Right>{formatCurrency(line.lineAmount)}</Right></TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  </div>
);

const StocktakeDetail = ({ sheet }: { sheet: StocktakeSheetDto }) => (
  <div className="space-y-5">
    <Meta>
      <MetaRow label="Ngày kiểm kê">{formatDate(sheet.stocktakeDate)}</MetaRow>
      <MetaRow label="Người lập">{sheet.createdByName ?? "—"}</MetaRow>
      <MetaRow label="Tạo lúc">{formatDateTime(sheet.createdAt)}</MetaRow>
      {sheet.note && <MetaRow label="Ghi chú">{sheet.note}</MetaRow>}
    </Meta>
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Sản phẩm kho</TableHead>
          <RightHead>Sổ sách</RightHead>
          <RightHead>Số đếm</RightHead>
          <RightHead>Chênh lệch</RightHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {sheet.lines.map((line) => (
          <TableRow key={line.stocktakeSheetLineId}>
            <ItemCell code={line.itemCode} name={line.itemName} />
            <TableCell><Right>{formatQuantity(line.bookQuantity, line.unit)}</Right></TableCell>
            <TableCell><Right>{formatQuantity(line.countedQuantity, line.unit)}</Right></TableCell>
            <TableCell>
              <Right>
                <span className={getSignedTextClass(line.difference)}>
                  {formatSignedQuantity(line.difference, line.unit)}
                </span>
              </Right>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  </div>
);

const WasteDetail = ({ sheet }: { sheet: WasteSheetDto }) => (
  <div className="space-y-5">
    <Meta>
      <MetaRow label="Ngày hủy hàng">{formatDate(sheet.wasteDate)}</MetaRow>
      <MetaRow label="Lý do">
        <Badge variant="outline">{getWasteReasonLabel(sheet.reason)}</Badge>
      </MetaRow>
      <MetaRow label="Giá trị hao hụt">{formatCurrency(sheet.totalAmount)}</MetaRow>
      <MetaRow label="Người lập">{sheet.createdByName ?? "—"}</MetaRow>
      <MetaRow label="Tạo lúc">{formatDateTime(sheet.createdAt)}</MetaRow>
      {sheet.note && <MetaRow label="Ghi chú">{sheet.note}</MetaRow>}
    </Meta>
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Sản phẩm kho</TableHead>
          <RightHead>Số lượng hủy</RightHead>
          <RightHead>Giá vốn</RightHead>
          <RightHead>Thành tiền</RightHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {sheet.lines.map((line) => (
          <TableRow key={line.wasteSheetLineId}>
            <ItemCell code={line.itemCode} name={line.itemName} />
            <TableCell><Right>{formatQuantity(line.quantity, line.unit)}</Right></TableCell>
            <TableCell><Right>{formatCurrency(line.unitCost)}</Right></TableCell>
            <TableCell><Right>{formatCurrency(line.lineAmount)}</Right></TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  </div>
);

export const StockReceiptDetailView = ({ id }: { id: number }) => (
  <DetailQuery query={useAdminStockReceipt(id)} title="Không tải được phiếu nhập kho">
    {(receipt) => <ReceiptDetail receipt={receipt} />}
  </DetailQuery>
);

export const StocktakeDetailView = ({ id }: { id: number }) => (
  <DetailQuery query={useAdminStocktake(id)} title="Không tải được phiếu kiểm kê">
    {(sheet) => <StocktakeDetail sheet={sheet} />}
  </DetailQuery>
);

export const WasteSheetDetailView = ({ id }: { id: number }) => (
  <DetailQuery query={useAdminWasteSheet(id)} title="Không tải được phiếu hao hụt">
    {(sheet) => <WasteDetail sheet={sheet} />}
  </DetailQuery>
);
