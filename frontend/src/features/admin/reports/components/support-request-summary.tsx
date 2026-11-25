import { formatNumber, formatPercent } from "@/lib/formatters";
import type { FinanceReportBlockDto } from "../types";
import { FinanceSummaryItem, toFinanceNumber } from "./finance-summary-item";

export const SUPPORT_REQUEST_BLOCK_KEY = "support-requests";

interface SupportRequestSummaryProps {
  block: FinanceReportBlockDto;
}

export const SupportRequestSummary = ({ block }: SupportRequestSummaryProps) => {
  const totals = block.totals;
  if (!totals) return null;

  const requests = toFinanceNumber(totals.requests) ?? 0;
  const resolved = toFinanceNumber(totals.resolved) ?? 0;
  const onTime = toFinanceNumber(totals.resolvedOnTime) ?? 0;
  const overdueOpen = toFinanceNumber(totals.overdueOpen) ?? 0;
  const averageHours = toFinanceNumber(totals.averageHours);
  const onTimeRate = toFinanceNumber(totals.onTimeRate);

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <FinanceSummaryItem
        label="Số yêu cầu"
        value={formatNumber(requests)}
        hint={`${formatNumber(resolved)} đã giải quyết`}
      />
      <FinanceSummaryItem
        label="Thời gian xử lý trung bình"
        value={
          averageHours == null ? "—" : `${formatNumber(averageHours, { maximumFractionDigits: 1 })} giờ`
        }
        hint="Từ lúc tạo đến lúc giải quyết"
      />
      <FinanceSummaryItem
        label="Tỷ lệ giải quyết trong hạn"
        value={onTimeRate == null ? "—" : formatPercent(onTimeRate)}
        hint={`${formatNumber(onTime)}/${formatNumber(resolved + overdueOpen)} yêu cầu`}
      />
      <FinanceSummaryItem
        label="Quá hạn chưa giải quyết"
        value={formatNumber(overdueOpen)}
      />
    </div>
  );
};
