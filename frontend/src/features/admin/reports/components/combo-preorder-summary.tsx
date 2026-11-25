import { formatCurrency, formatNumber, formatPercent } from "@/lib/formatters";
import type { FinanceReportBlockDto } from "../types";
import { FinanceSummaryItem, toFinanceNumber } from "./finance-summary-item";

export const COMBO_PREORDER_BLOCK_KEY = "combo-preorder";

interface ComboPreorderSummaryProps {
  block: FinanceReportBlockDto;
}

export const ComboPreorderSummary = ({ block }: ComboPreorderSummaryProps) => {
  const totals = block.totals;
  if (!totals) return null;

  const rate = toFinanceNumber(totals.supplementRate);
  const preorderRevenue = toFinanceNumber(totals.preorderRevenue) ?? 0;
  const counterRevenue = toFinanceNumber(totals.counterRevenue) ?? 0;
  const share = toFinanceNumber(totals.preorderShare);
  const pickupRate = toFinanceNumber(totals.preorderPickupRate);

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <FinanceSummaryItem
        label="Tỷ lệ khách mua thêm combo"
        value={rate == null ? "—" : formatPercent(rate)}
        hint={`${formatNumber(toFinanceNumber(totals.supplementedOrders) ?? 0)}/${formatNumber(toFinanceNumber(totals.ticketOrders) ?? 0)} đơn vé`}
      />
      <FinanceSummaryItem
        label="Doanh thu combo đặt trước"
        value={formatCurrency(preorderRevenue)}
        hint={`${formatNumber(toFinanceNumber(totals.preorderUnits) ?? 0)} combo`}
      />
      <FinanceSummaryItem
        label="So với combo mua tại quầy"
        value={share == null ? "—" : formatPercent(share)}
        hint={`Quầy ${formatCurrency(counterRevenue)}`}
      />
      <FinanceSummaryItem
        label="Combo đặt trước đã nhận"
        value={pickupRate == null ? "—" : formatPercent(pickupRate)}
        hint={`${formatNumber(toFinanceNumber(totals.preorderPickedUp) ?? 0)}/${formatNumber(toFinanceNumber(totals.preorderUnits) ?? 0)} combo`}
      />
    </div>
  );
};
