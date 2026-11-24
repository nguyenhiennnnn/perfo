import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { FinanceBlockTable } from "@/features/admin/reports/components/finance-block-table";
import { ShiftStatusBadge } from "@/features/admin/counter-shifts/components/counter-shift-columns";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import type { DailyClosingSnapshot } from "../types";

const CHANNEL_LABELS: Record<string, string> = { Online: "Online", Counter: "Quầy" };
const METHOD_LABELS: Record<string, string> = {
  VNPay: "VNPay",
  MoMo: "MoMo",
  Stripe: "Stripe",
  Cash: "Tiền mặt",
  GiftCard: "Thẻ quà tặng",
  BankTransfer: "Chuyển khoản",
};

const StatCard = ({ label, value, hint }: { label: string; value: string; hint?: string }) => (
  <div className="bg-card p-4">
    <p className="text-xs text-muted-foreground">{label}</p>
    <p className="mt-1 text-lg font-semibold tabular-nums text-foreground">{value}</p>
    {hint ? <p className="mt-0.5 text-xs text-muted-foreground">{hint}</p> : null}
  </div>
);

const Row = ({ label, value, strong }: { label: string; value: string; strong?: boolean }) => (
  <>
    <dt className={cn("text-muted-foreground", strong && "font-medium text-foreground")}>{label}</dt>
    <dd className={cn("text-right tabular-nums", strong ? "font-semibold" : "font-medium")}>{value}</dd>
  </>
);

interface DailyClosingSummaryProps {
  snapshot: DailyClosingSnapshot;
}

export const DailyClosingSummary = ({ snapshot }: DailyClosingSummaryProps) => {
  const { revenue, discounts, refunds, methods, orders, cash, blocks } = snapshot;

  return (
    <div className="space-y-4">
      <p className="text-xs text-muted-foreground">
        Số liệu dựng lúc {formatDateTime(snapshot.generatedAt)}
      </p>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatCard label="Doanh thu vé" value={formatCurrency(revenue.ticketRevenue)} />
        <StatCard label="Doanh thu combo" value={formatCurrency(revenue.comboRevenue)} />
        <StatCard
          label="Tổng doanh thu"
          value={formatCurrency(revenue.totalRevenue)}
          hint="Sau giảm giá và hoàn tiền"
        />
        <StatCard label="Tiền mặt phải có" value={formatCurrency(cash.expectedCash)} />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Doanh thu, giảm giá, hoàn tiền</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-2 text-sm">
              <Row label="Thẻ quà tặng đã bán" value={formatCurrency(revenue.giftCardSold)} />
              <Row label="Thẻ quà tặng đã dùng" value={formatCurrency(revenue.giftCardUsed)} />
              <Row label="Ưu đãi hạng thành viên" value={formatCurrency(discounts.tierDiscount)} />
              <Row label="Khuyến mãi" value={formatCurrency(discounts.promotionDiscount)} />
              <Row label="Giảm giá khác" value={formatCurrency(discounts.otherDiscount)} />
              <Row label="Tổng giảm giá" value={formatCurrency(discounts.totalDiscount)} strong />
              <Row label="Hoàn tiền vé" value={formatCurrency(refunds.ticketRefundAmount)} />
              <Row label="Tiền đã hoàn qua thanh toán" value={formatCurrency(refunds.paymentRefunded)} />
              <Row label="Đơn hủy" value={String(refunds.cancelledOrders)} />
            </dl>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Tiền mặt tại quầy</CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-2 text-sm">
              <Row label="Tiền đầu ca" value={formatCurrency(cash.openingCash)} />
              <Row label="Thu tiền mặt" value={formatCurrency(cash.collected)} />
              <Row label="Hoàn tiền mặt" value={formatCurrency(cash.refunded)} />
              <Row label="Tiền thối" value={formatCurrency(cash.changeGiven)} />
              <Row label="Phải có theo sổ sách" value={formatCurrency(cash.expectedCash)} strong />
              <Row label="Số ca" value={`${cash.shiftCount} (đang mở ${cash.openShiftCount})`} />
              <Row label="Đơn chờ thanh toán" value={String(orders.pendingOrders)} />
              <Row label="Đơn lỗi" value={String(orders.problemOrders)} />
            </dl>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Ca quầy trong ngày</CardTitle>
        </CardHeader>
        <CardContent>
          {cash.shifts.length === 0 ? (
            <p className="text-sm text-muted-foreground">Không có ca quầy nào trong ngày.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nhân viên</TableHead>
                  <TableHead>Trạng thái</TableHead>
                  <TableHead className="text-right">Tiền đầu ca</TableHead>
                  <TableHead className="text-right">Phải có</TableHead>
                  <TableHead className="text-right">Thực đếm</TableHead>
                  <TableHead className="text-right">Chênh lệch</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {cash.shifts.map((shift) => (
                  <TableRow key={shift.counterShiftId}>
                    <TableCell className="font-medium">{shift.employeeName}</TableCell>
                    <TableCell>
                      <ShiftStatusBadge status={shift.status} />
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatCurrency(shift.openingCash)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatCurrency(shift.expectedCash)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {shift.countedCash == null ? "—" : formatCurrency(shift.countedCash)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {shift.variance == null ? "—" : formatCurrency(shift.variance)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Thu theo kênh và phương thức</CardTitle>
        </CardHeader>
        <CardContent>
          {methods.length === 0 ? (
            <p className="text-sm text-muted-foreground">Chưa có giao dịch nào trong ngày.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Kênh</TableHead>
                  <TableHead>Phương thức</TableHead>
                  <TableHead className="text-right">Giao dịch</TableHead>
                  <TableHead className="text-right">Tiền thu</TableHead>
                  <TableHead className="text-right">Tiền hoàn</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {methods.map((item, index) => (
                  <TableRow key={`${item.channel}-${item.method ?? "none"}-${index}`}>
                    <TableCell>
                      <Badge variant="outline">{CHANNEL_LABELS[item.channel] ?? item.channel}</Badge>
                    </TableCell>
                    <TableCell>
                      {item.method ? (METHOD_LABELS[item.method] ?? item.method) : "—"}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">{item.transactionCount}</TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatCurrency(item.collected)}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatCurrency(item.refunded)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {blocks
        .filter((block) => block.rows.length > 0)
        .map((block) => (
          <FinanceBlockTable key={block.key} block={block} />
        ))}
    </div>
  );
};
