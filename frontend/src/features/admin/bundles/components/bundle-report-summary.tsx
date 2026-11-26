import { formatCurrency, formatNumber, formatPercent } from "@/lib/formatters";
import {
  FinanceSummaryItem,
  toFinanceNumber,
} from "@/features/admin/reports/components/finance-summary-item";
import type { FinanceReportBlockDto } from "@/features/admin/reports/types";

interface BundleReportSummaryProps {
  block: FinanceReportBlockDto;
}

export const BundleReportSummary = ({ block }: BundleReportSummaryProps) => {
  const totals = block.totals;
  if (!totals) return null;

  const sold = toFinanceNumber(totals.sold) ?? 0;
  const retail = toFinanceNumber(totals.retail) ?? 0;
  const revenue = toFinanceNumber(totals.revenue) ?? 0;
  const savings = toFinanceNumber(totals.savings) ?? 0;
  const net = toFinanceNumber(totals.netRevenue) ?? 0;
  const viewers = toFinanceNumber(totals.viewers) ?? 0;
  const buyers = toFinanceNumber(totals.buyers) ?? 0;
  const conversion = toFinanceNumber(totals.conversion);

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <FinanceSummaryItem
        label="Số gói đã bán"
        value={formatNumber(sold)}
        hint={`Doanh thu thực thu ${formatCurrency(net)}`}
      />
      <FinanceSummaryItem
        label="Doanh thu gói"
        value={formatCurrency(revenue)}
        hint="Trước hoàn tiền"
      />
      <FinanceSummaryItem
        label="So với mua lẻ"
        value={formatCurrency(retail)}
        hint={`Khách tiết kiệm ${formatCurrency(savings)}`}
      />
      <FinanceSummaryItem
        label="Tỷ lệ chuyển đổi"
        value={conversion == null ? "—" : formatPercent(conversion)}
        hint={`${formatNumber(buyers)}/${formatNumber(viewers)} lượt xem gói rồi mua`}
      />
    </div>
  );
};
