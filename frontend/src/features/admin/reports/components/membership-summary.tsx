import { formatCurrency, formatNumber, formatPercent } from "@/lib/formatters";
import type { FinanceReportBlockDto } from "../types";
import { FinanceSummaryItem, toFinanceNumber } from "./finance-summary-item";

export const MEMBERSHIP_SUMMARY_BLOCK_KEY = "membership-summary";

interface MembershipSummaryProps {
  block: FinanceReportBlockDto;
}

export const MembershipSummary = ({ block }: MembershipSummaryProps) => {
  const totals = block.totals;
  if (!totals) return null;

  const renewalRate = toFinanceNumber(totals.renewalRate);
  const benefitRatio = toFinanceNumber(totals.benefitRatio);
  const recognized = toFinanceNumber(totals.recognized) ?? 0;
  const deferred = toFinanceNumber(totals.deferred) ?? 0;
  const benefitUsed = toFinanceNumber(totals.benefitUsed) ?? 0;
  const vsRetail = toFinanceNumber(totals.vsRetail) ?? 0;

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
      <FinanceSummaryItem
        label="Hội viên đang hoạt động"
        value={formatNumber(toFinanceNumber(totals.activeMembers) ?? 0)}
        hint="Tại cuối kỳ"
      />
      <FinanceSummaryItem
        label="Tỷ lệ gia hạn"
        value={renewalRate == null ? "—" : formatPercent(renewalRate)}
        hint={`${formatNumber(toFinanceNumber(totals.renewed) ?? 0)}/${formatNumber(toFinanceNumber(totals.renewalDue) ?? 0)} gói hết hạn trong kỳ`}
      />
      <FinanceSummaryItem
        label="Quyền lợi đã dùng / tiền gói"
        value={benefitRatio == null ? "—" : formatPercent(benefitRatio)}
        hint={`${formatCurrency(benefitUsed)} / ${formatCurrency(recognized)} đã ghi nhận`}
      />
      <FinanceSummaryItem
        label="Lợi nhuận so với bán vé lẻ"
        value={formatCurrency(vsRetail)}
        hint="Tiền gói đã ghi nhận trừ quyền lợi đã dùng"
      />
      <FinanceSummaryItem
        label="Doanh thu gói đã ghi nhận"
        value={formatCurrency(recognized)}
        hint="Trong kỳ, theo bút toán ghi nhận hằng tháng"
      />
      <FinanceSummaryItem
        label="Doanh thu gói chưa ghi nhận"
        value={formatCurrency(deferred)}
        hint="Khoản nhận trước tại cuối kỳ"
      />
    </div>
  );
};
