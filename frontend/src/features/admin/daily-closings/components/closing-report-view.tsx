import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { SignedAmount } from "@/features/admin/ledger-adjustments/components/signed-amount";
import { getLedgerAdjustmentTypeLabel } from "@/features/admin/ledger-adjustments/utils/labels";
import { formatCurrency, formatDate } from "@/lib/formatters";
import { cn } from "@/lib/utils";

import type { DailyClosingFigures, DailyClosingReport } from "../types";

interface FigureRow {
  label: string;
  base: number;
  delta: number;
  adjusted: number;
  count?: boolean;
  strong?: boolean;
}

const buildRows = (report: DailyClosingReport): FigureRow[] => {
  const { base, adjusted, adjustmentTotals: t } = report;
  const row = (
    label: string,
    pick: (f: DailyClosingFigures) => number,
    delta: number,
    options?: Pick<FigureRow, "count" | "strong">,
  ): FigureRow => ({
    label,
    base: pick(base),
    delta,
    adjusted: pick(adjusted),
    ...options,
  });

  return [
    row("Doanh thu vé", (f) => f.ticketRevenue, t.ticketRevenue),
    row("Doanh thu combo", (f) => f.comboRevenue, t.comboRevenue),
    row("Tổng doanh thu", (f) => f.totalRevenue, t.ticketRevenue + t.comboRevenue, { strong: true }),
    row("Thẻ quà tặng đã bán", (f) => f.giftCardSold, t.giftCardSold),
    row("Thẻ quà tặng đã dùng", (f) => f.giftCardUsed, 0),
    row("Tổng giảm giá", (f) => f.totalDiscount, t.discount),
    row("Hoàn tiền vé", (f) => f.ticketRefundAmount, t.refundAmount),
    row("Đơn hủy", (f) => f.cancelledOrders, t.cancelledOrders, { count: true }),
  ];
};

const formatValue = (value: number, count?: boolean) =>
  count ? String(value) : formatCurrency(value);

interface ClosingReportViewProps {
  report: DailyClosingReport;
  isRefreshing?: boolean;
}

export const ClosingReportView = ({ report, isRefreshing }: ClosingReportViewProps) => {
  const isPosting = report.mode === "PostingDate";
  const rows = buildRows(report);

  return (
    <div className={cn("space-y-4 transition-opacity", isRefreshing && "opacity-60")}>
      <p className="bg-muted p-3 text-sm text-muted-foreground">
        {isPosting
          ? "Theo ngày ghi sổ: số liệu chốt của ngày cộng các bút toán được ghi vào ngày này, kể cả bút toán của những ngày gốc trước đó."
          : "Theo ngày gốc: số liệu chốt của ngày cộng mọi bút toán điều chỉnh phát sinh sau cho đúng ngày này."}
      </p>

      <Card>
        <CardHeader>
          <CardTitle>Số liệu {isPosting ? "theo ngày ghi sổ" : "theo ngày gốc"}</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Chỉ tiêu</TableHead>
                <TableHead className="text-right">Số chốt</TableHead>
                <TableHead className="text-right">Điều chỉnh</TableHead>
                <TableHead className="text-right">Sau điều chỉnh</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.label}>
                  <TableCell className={cn(row.strong && "font-semibold")}>{row.label}</TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatValue(row.base, row.count)}
                  </TableCell>
                  <TableCell className="text-right">
                    <SignedAmount value={row.delta} count={row.count} />
                  </TableCell>
                  <TableCell
                    className={cn("text-right tabular-nums", row.strong && "font-semibold")}
                  >
                    {formatValue(row.adjusted, row.count)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Bút toán điều chỉnh ({report.adjustmentTotals.count})</CardTitle>
        </CardHeader>
        <CardContent>
          {report.adjustments.length === 0 ? (
            <p className="text-sm text-muted-foreground">Không có bút toán nào.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Ghi sổ</TableHead>
                  <TableHead>Ngày gốc</TableHead>
                  <TableHead>Loại</TableHead>
                  <TableHead className="text-right">Vé</TableHead>
                  <TableHead className="text-right">Combo</TableHead>
                  <TableHead className="text-right">Thẻ quà</TableHead>
                  <TableHead className="text-right">Giảm giá</TableHead>
                  <TableHead className="text-right">Hoàn tiền</TableHead>
                  <TableHead className="text-right">Đơn hủy</TableHead>
                  <TableHead>Lý do</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {report.adjustments.map((a) => (
                  <TableRow key={a.ledgerAdjustmentId}>
                    <TableCell className="tabular-nums">{formatDate(a.postingDate)}</TableCell>
                    <TableCell className="tabular-nums">{formatDate(a.originalDate)}</TableCell>
                    <TableCell>
                      <Badge variant={a.type === "Manual" ? "outline" : "secondary"}>
                        {getLedgerAdjustmentTypeLabel(a.type)}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      <SignedAmount value={a.ticketRevenueDelta} />
                    </TableCell>
                    <TableCell className="text-right">
                      <SignedAmount value={a.comboRevenueDelta} />
                    </TableCell>
                    <TableCell className="text-right">
                      <SignedAmount value={a.giftCardSoldDelta} />
                    </TableCell>
                    <TableCell className="text-right">
                      <SignedAmount value={a.discountDelta} />
                    </TableCell>
                    <TableCell className="text-right">
                      <SignedAmount value={a.refundAmountDelta} />
                    </TableCell>
                    <TableCell className="text-right">
                      <SignedAmount value={a.cancelledOrdersDelta} count />
                    </TableCell>
                    <TableCell className="max-w-56 whitespace-normal text-sm">{a.reason}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
              <TableFooter>
                <TableRow>
                  <TableCell colSpan={3} className="font-semibold">
                    Tổng
                  </TableCell>
                  <TableCell className="text-right">
                    <SignedAmount value={report.adjustmentTotals.ticketRevenue} />
                  </TableCell>
                  <TableCell className="text-right">
                    <SignedAmount value={report.adjustmentTotals.comboRevenue} />
                  </TableCell>
                  <TableCell className="text-right">
                    <SignedAmount value={report.adjustmentTotals.giftCardSold} />
                  </TableCell>
                  <TableCell className="text-right">
                    <SignedAmount value={report.adjustmentTotals.discount} />
                  </TableCell>
                  <TableCell className="text-right">
                    <SignedAmount value={report.adjustmentTotals.refundAmount} />
                  </TableCell>
                  <TableCell className="text-right">
                    <SignedAmount value={report.adjustmentTotals.cancelledOrders} count />
                  </TableCell>
                  <TableCell />
                </TableRow>
              </TableFooter>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
};
