import { formatCurrency } from "@/lib/formatters";
import { ExchangeDifference } from "./exchange-difference";

interface ExchangeSummaryCardProps {
  oldAmount: number;
  newAmount: number;
  difference: number;
  title?: string;
}

export const ExchangeSummaryCard = ({
  oldAmount,
  newAmount,
  difference,
  title = "Chênh lệch giá",
}: ExchangeSummaryCardProps) => (
  <section className="cut-md bg-card p-4">
    <h3 className="t-h3 text-foreground">{title}</h3>
    <dl className="mt-3 grid grid-cols-[1fr_auto] gap-x-4 gap-y-2 text-sm">
      <dt className="text-muted-foreground">Đã trả cho vé cũ</dt>
      <dd className="text-right font-medium tabular-nums text-foreground">{formatCurrency(oldAmount)}</dd>
      <dt className="text-muted-foreground">Giá vé suất mới</dt>
      <dd className="text-right font-medium tabular-nums text-foreground">{formatCurrency(newAmount)}</dd>
    </dl>
    <div className="mt-3 border-t border-border pt-3 text-right text-base">
      <ExchangeDifference difference={difference} />
    </div>
    {difference < 0 && (
      <p className="mt-2 text-xs text-muted-foreground">
        Phần chênh lệch được cấp dưới dạng thẻ tín dụng đổi suất, không hoàn về cổng thanh toán.
      </p>
    )}
  </section>
);
